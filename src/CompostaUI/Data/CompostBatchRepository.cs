using Composta.Enums;
using Composta.Interfaces;
using Composta.Models;
using Npgsql;
using NpgsqlTypes;

namespace Composta.Data
{
    public class CompostBatchRepository : PostgresRepository<CompostBatch>, IRepository<CompostBatch>
    {
        public CompostBatchRepository(string connstring) : base(connstring)
        {
        }

        public List<CompostBatch> GetAll()
        {
            return GetByUser(null);
        }

        public List<CompostBatch> GetByUser(string? userId)
        {
            using NpgsqlConnection conn = OpenConnection();

            List<CompostBatch> batches = ExecuteSelect(conn, "SELECT * FROM st_select_batches(:_user_id)", MapRow,
                p => p.AddWithValue("_user_id", NpgsqlDbType.Varchar, DbValue(userId)));

            foreach (CompostBatch batch in batches)
            {
                List<WasteItem> items = ExecuteSelect(conn, "SELECT * FROM st_select_waste_items(:_batch_id)",
                    MapWasteItem, p => p.AddWithValue("_batch_id", batch.Id));
                foreach (WasteItem item in items)
                {
                    batch.AddWasteItem(item);
                }

                List<MaintenanceSchedule> schedules = ExecuteSelect(conn, "SELECT * FROM st_select_schedules(:_batch_id)",
                    MapSchedule, p => p.AddWithValue("_batch_id", batch.Id));
                foreach (MaintenanceSchedule schedule in schedules)
                {
                    batch.AddSchedule(schedule);
                }
            }

            return batches;
        }

        public CompostBatch? GetById(string id)
        {
            return GetAll().FirstOrDefault(b => b.Id == id);
        }

        public void Add(CompostBatch entity)
        {
            ArgumentNullException.ThrowIfNull(entity);

            using NpgsqlConnection conn = OpenConnection();
            using NpgsqlTransaction tx = conn.BeginTransaction();

            EnsureSuccess(ExecuteFunction(conn, tx,
                "SELECT st_insert_batch(:_id, :_user_id, :_start_date, :_status, :_brown_green_ratio, :_estimated_maturity_date)",
                p =>
                {
                    p.AddWithValue("_id", entity.Id);
                    p.AddWithValue("_user_id", entity.UserId);
                    p.AddWithValue("_start_date", entity.StartDate);
                    p.AddWithValue("_status", entity.Status.ToString());
                    p.AddWithValue("_brown_green_ratio", ToNumeric(entity.BrownGreenRatio));
                    p.AddWithValue("_estimated_maturity_date", entity.EstimatedMaturityDate);
                }), "Gagal menambahkan batch ke database.");

            foreach (WasteItem item in entity.WasteItems)
            {
                EnsureSuccess(InsertWasteItem(conn, tx, entity.Id, item), "Gagal menyimpan sampah ke database.");
            }

            tx.Commit();
        }

        public void Update(CompostBatch entity)
        {
            ArgumentNullException.ThrowIfNull(entity);

            using NpgsqlConnection conn = OpenConnection();
            EnsureSuccess(UpdateBatch(conn, null, entity), "Gagal memperbarui batch di database.");
        }

        public void Delete(string id)
        {
            EnsureSuccess(ExecuteFunction("SELECT st_delete_batch(:_id)", p => p.AddWithValue("_id", id)),
                "Gagal menghapus batch dari database.");
        }

        public void AddWasteItem(CompostBatch batch, WasteItem item)
        {
            ArgumentNullException.ThrowIfNull(batch);
            ArgumentNullException.ThrowIfNull(item);

            if (item is NonCompostableWaste)
            {
                throw new InvalidOperationException("Sampah anorganik tidak dapat dimasukkan ke batch kompos.");
            }

            using NpgsqlConnection conn = OpenConnection();
            using NpgsqlTransaction tx = conn.BeginTransaction();

            batch.AddWasteItem(item);
            try
            {
                EnsureSuccess(InsertWasteItem(conn, tx, batch.Id, item), "Gagal menyimpan sampah ke database.");
                EnsureSuccess(UpdateBatch(conn, tx, batch), "Gagal memperbarui rasio batch di database.");
                tx.Commit();
            }
            catch
            {
                batch.WasteItems.Remove(item);
                batch.BrownGreenRatio = batch.CalculateRatio();
                throw;
            }
        }

        protected override CompostBatch MapRow(NpgsqlDataReader rd)
        {
            CompostBatch batch = new CompostBatch();
            batch.Id = (string)rd["id"];
            batch.UserId = (string)rd["user_id"];
            batch.StartDate = (DateTime)rd["start_date"];
            batch.Status = Enum.Parse<BatchStatus>((string)rd["status"]);
            batch.BrownGreenRatio = Convert.ToDouble(rd["brown_green_ratio"]);
            batch.EstimatedMaturityDate = (DateTime)rd["estimated_maturity_date"];
            return batch;
        }

        private static WasteItem MapWasteItem(NpgsqlDataReader rd)
        {
            WasteCategory? category = null;
            if (rd["category_id"] is string categoryId)
            {
                category = new WasteCategory((string)rd["category_name"],
                    Convert.ToDouble(rd["co2e_factor_per_kg"]), (bool)rd["is_compostable"]);
                category.Id = categoryId;
            }

            string name = (string)rd["name"];
            double weight = Convert.ToDouble(rd["weight"]);

            WasteItem item;
            if ((string)rd["item_type"] == "compostable")
            {
                BrownGreenType type = Enum.Parse<BrownGreenType>(rd["brown_green_type"] as string ?? nameof(BrownGreenType.Green));
                double moisture = rd["moisture_content"] is DBNull ? 50 : Convert.ToDouble(rd["moisture_content"]);
                item = new CompostableWaste(name, weight, category, type, moisture);
            }
            else
            {
                item = new NonCompostableWaste(name, weight, category, rd["reason"] as string ?? string.Empty);
            }

            item.Id = (string)rd["id"];
            item.DateAdded = (DateTime)rd["date_added"];
            return item;
        }

        private static MaintenanceSchedule MapSchedule(NpgsqlDataReader rd)
        {
            MaintenanceSchedule schedule = new MaintenanceSchedule(
                Enum.Parse<TaskType>((string)rd["task_type"]), (DateTime)rd["due_date"]);
            schedule.Id = (string)rd["id"];
            schedule.IsCompleted = (bool)rd["is_completed"];
            return schedule;
        }

        private static int InsertWasteItem(NpgsqlConnection conn, NpgsqlTransaction? tx, string batchId, WasteItem item)
        {
            CompostableWaste? compostable = item as CompostableWaste;
            NonCompostableWaste? nonCompostable = item as NonCompostableWaste;

            return ExecuteFunction(conn, tx,
                "SELECT st_insert_waste_item(:_id, :_batch_id, :_category_id, :_item_type, :_name, :_weight, " +
                ":_date_added, :_brown_green_type, :_moisture_content, :_reason)",
                p =>
                {
                    p.AddWithValue("_id", item.Id);
                    p.AddWithValue("_batch_id", batchId);
                    p.AddWithValue("_category_id", NpgsqlDbType.Varchar, DbValue(item.Category?.Id));
                    p.AddWithValue("_item_type", compostable != null ? "compostable" : "noncompostable");
                    p.AddWithValue("_name", item.Name);
                    p.AddWithValue("_weight", ToNumeric(item.Weight));
                    p.AddWithValue("_date_added", item.DateAdded);
                    p.AddWithValue("_brown_green_type", NpgsqlDbType.Varchar, DbValue(compostable?.BrownGreenType.ToString()));
                    p.AddWithValue("_moisture_content", NpgsqlDbType.Numeric,
                        compostable != null ? (object)ToNumeric(compostable.MoistureContent) : DBNull.Value);
                    p.AddWithValue("_reason", NpgsqlDbType.Varchar, DbValue(nonCompostable?.Reason));
                });
        }

        private static int UpdateBatch(NpgsqlConnection conn, NpgsqlTransaction? tx, CompostBatch batch)
        {
            return ExecuteFunction(conn, tx,
                "SELECT st_update_batch(:_id, :_status, :_brown_green_ratio, :_estimated_maturity_date)",
                p =>
                {
                    p.AddWithValue("_id", batch.Id);
                    p.AddWithValue("_status", batch.Status.ToString());
                    p.AddWithValue("_brown_green_ratio", ToNumeric(batch.BrownGreenRatio));
                    p.AddWithValue("_estimated_maturity_date", batch.EstimatedMaturityDate);
                });
        }

        private static decimal ToNumeric(double value)
        {
            return Math.Round((decimal)value, 2);
        }
    }
}
