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

            if (entity.Schedules.Count == 0)
            {
                entity.GenerateDefaultSchedules();
            }

            foreach (MaintenanceSchedule schedule in entity.Schedules)
            {
                EnsureSuccess(InsertSchedule(conn, tx, entity.Id, schedule), "Gagal menyimpan jadwal perawatan ke database.");
            }

            tx.Commit();
        }

        public void Update(CompostBatch entity)
        {
            ArgumentNullException.ThrowIfNull(entity);
            Update(entity, entity.UserId);
        }

        public void Update(CompostBatch batch, string actingUserId)
        {
            ArgumentNullException.ThrowIfNull(batch);

            using NpgsqlConnection conn = OpenConnection();
            EnsureSuccess(UpdateBatch(conn, null, batch, actingUserId), "Gagal memperbarui batch di database.");
        }

        public void Delete(string id)
        {
            CompostBatch batch = GetById(id) ?? throw new InvalidOperationException("Batch tidak ditemukan di database.");
            Delete(batch, batch.UserId);
        }

        public void Delete(CompostBatch batch, string actingUserId)
        {
            ArgumentNullException.ThrowIfNull(batch);

            EnsureSuccess(ExecuteFunction("SELECT st_delete_batch(:_id, :_user_id)", p =>
                {
                    p.AddWithValue("_id", batch.Id);
                    p.AddWithValue("_user_id", actingUserId);
                }),
                "Batch tidak ditemukan atau bukan milik profil yang sedang masuk.");
        }

        public void AddWasteItem(CompostBatch batch, WasteItem item, int? maturityDays = null)
        {
            ArgumentNullException.ThrowIfNull(batch);
            AddWasteItem(batch, item, batch.UserId, maturityDays);
        }

        public void AddWasteItem(CompostBatch batch, WasteItem item, string actingUserId, int? maturityDays = null)
        {
            ArgumentNullException.ThrowIfNull(batch);
            ArgumentNullException.ThrowIfNull(item);

            if (item is NonCompostableWaste)
            {
                throw new InvalidOperationException("Sampah anorganik tidak dapat dimasukkan ke batch kompos.");
            }

            if (batch.Status != BatchStatus.Active)
            {
                throw new InvalidOperationException("Sampah hanya dapat ditambahkan ke batch yang masih berstatus Aktif.");
            }

            using NpgsqlConnection conn = OpenConnection();
            using NpgsqlTransaction tx = conn.BeginTransaction();

            DateTime previousMaturityDate = batch.EstimatedMaturityDate;
            Dictionary<string, DateTime> previousHarvestDueDates = batch.Schedules
                .Where(s => s.TaskType == TaskType.Harvest && !s.IsCompleted)
                .ToDictionary(s => s.Id, s => s.DueDate);

            batch.AddWasteItem(item);
            if (maturityDays.HasValue)
            {
                batch.RecalculateEstimatedMaturityDate(maturityDays.Value);
            }

            try
            {
                EnsureSuccess(InsertWasteItem(conn, tx, batch.Id, item), "Gagal menyimpan sampah ke database.");
                EnsureSuccess(UpdateBatch(conn, tx, batch, actingUserId), "Gagal memperbarui rasio batch di database.");
                tx.Commit();
            }
            catch
            {
                batch.WasteItems.Remove(item);
                batch.BrownGreenRatio = batch.CalculateRatio();
                batch.EstimatedMaturityDate = previousMaturityDate;
                foreach (MaintenanceSchedule schedule in batch.Schedules.Where(s => previousHarvestDueDates.ContainsKey(s.Id)))
                {
                    schedule.DueDate = previousHarvestDueDates[schedule.Id];
                }
                throw;
            }
        }

        public void Harvest(CompostBatch batch)
        {
            ArgumentNullException.ThrowIfNull(batch);
            Harvest(batch, batch.UserId);
        }

        public void Harvest(CompostBatch batch, string actingUserId)
        {
            ArgumentNullException.ThrowIfNull(batch);

            if (batch.Status != BatchStatus.Ready)
            {
                throw new InvalidOperationException("Hanya batch berstatus Siap Panen yang dapat dipanen.");
            }

            List<MaintenanceSchedule> pending = batch.Schedules.Where(s => !s.IsCompleted).ToList();

            using NpgsqlConnection conn = OpenConnection();
            using NpgsqlTransaction tx = conn.BeginTransaction();

            batch.Status = BatchStatus.Archived;
            try
            {
                EnsureSuccess(UpdateBatch(conn, tx, batch, actingUserId), "Gagal memperbarui batch di database.");
                foreach (MaintenanceSchedule schedule in pending)
                {
                    EnsureSuccess(ExecuteFunction(conn, tx, "SELECT st_complete_schedule(:_id)",
                        p => p.AddWithValue("_id", schedule.Id)),
                        "Gagal memperbarui status jadwal perawatan di database.");
                }

                tx.Commit();
            }
            catch
            {
                batch.Status = BatchStatus.Ready;
                throw;
            }

            foreach (MaintenanceSchedule schedule in pending)
            {
                schedule.MarkCompleted();
            }
        }

        public List<MaintenanceSchedule> CompleteScheduleWithEarlier(CompostBatch batch, MaintenanceSchedule schedule)
        {
            ArgumentNullException.ThrowIfNull(batch);
            ArgumentNullException.ThrowIfNull(schedule);

            List<MaintenanceSchedule> toComplete = batch.Schedules
                .Where(s => !s.IsCompleted
                    && s.TaskType == schedule.TaskType
                    && s.DueDate <= schedule.DueDate
                    && s.Id != schedule.Id)
                .Append(schedule)
                .ToList();

            using NpgsqlConnection conn = OpenConnection();
            using NpgsqlTransaction tx = conn.BeginTransaction();

            foreach (MaintenanceSchedule item in toComplete)
            {
                EnsureSuccess(ExecuteFunction(conn, tx, "SELECT st_complete_schedule(:_id)",
                    p => p.AddWithValue("_id", item.Id)),
                    "Gagal memperbarui status jadwal perawatan di database.");
            }

            tx.Commit();

            foreach (MaintenanceSchedule item in toComplete)
            {
                item.MarkCompleted();
            }

            return toComplete;
        }

        public void AddSchedule(CompostBatch batch, MaintenanceSchedule schedule)
        {
            ArgumentNullException.ThrowIfNull(batch);
            ArgumentNullException.ThrowIfNull(schedule);

            using NpgsqlConnection conn = OpenConnection();
            using NpgsqlTransaction tx = conn.BeginTransaction();

            batch.AddSchedule(schedule);
            try
            {
                EnsureSuccess(InsertSchedule(conn, tx, batch.Id, schedule), "Gagal menyimpan jadwal perawatan ke database.");
                tx.Commit();
            }
            catch
            {
                batch.Schedules.Remove(schedule);
                throw;
            }
        }

        public void CompleteSchedule(MaintenanceSchedule schedule)
        {
            ArgumentNullException.ThrowIfNull(schedule);

            EnsureSuccess(ExecuteFunction("SELECT st_complete_schedule(:_id)", p => p.AddWithValue("_id", schedule.Id)),
                "Gagal memperbarui status jadwal perawatan di database.");
            schedule.MarkCompleted();
        }

        public List<CO2eTimelinePoint> GetCO2eTimeline(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return new List<CO2eTimelinePoint>();
            }

            using NpgsqlConnection conn = OpenConnection();
            return ExecuteSelect(conn, "SELECT * FROM st_select_co2e_timeline(:_user_id)",
                MapCO2eTimelinePoint,
                p => p.AddWithValue("_user_id", NpgsqlDbType.Varchar, userId));
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

        private static CO2eTimelinePoint MapCO2eTimelinePoint(NpgsqlDataReader rd)
        {
            DateTime date = rd["tanggal"] is DateOnly dateOnly
                ? dateOnly.ToDateTime(TimeOnly.MinValue)
                : Convert.ToDateTime(rd["tanggal"]);
            double daily = Convert.ToDouble(rd["co2e_harian"]);
            double cumulative = Convert.ToDouble(rd["co2e_akumulatif"]);
            return new CO2eTimelinePoint(date, daily, cumulative);
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

        private static int InsertSchedule(NpgsqlConnection conn, NpgsqlTransaction? tx, string batchId, MaintenanceSchedule schedule)
        {
            return ExecuteFunction(conn, tx,
                "SELECT st_insert_schedule(:_id, :_batch_id, :_task_type, :_due_date, :_is_completed)",
                p =>
                {
                    p.AddWithValue("_id", schedule.Id);
                    p.AddWithValue("_batch_id", batchId);
                    p.AddWithValue("_task_type", schedule.TaskType.ToString());
                    p.AddWithValue("_due_date", schedule.DueDate);
                    p.AddWithValue("_is_completed", schedule.IsCompleted);
                });
        }

        private static int UpdateBatch(NpgsqlConnection conn, NpgsqlTransaction? tx, CompostBatch batch, string actingUserId)
        {
            return ExecuteFunction(conn, tx,
                "SELECT st_update_batch(:_id, :_user_id, :_status, :_brown_green_ratio, :_estimated_maturity_date)",
                p =>
                {
                    p.AddWithValue("_id", batch.Id);
                    p.AddWithValue("_user_id", actingUserId);
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
