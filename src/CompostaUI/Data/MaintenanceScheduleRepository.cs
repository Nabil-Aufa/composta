using Composta.Enums;
using Composta.Interfaces;
using Composta.Models;
using Npgsql;

namespace Composta.Data
{
    public class MaintenanceScheduleRepository : PostgresRepository<MaintenanceSchedule>, IRepository<MaintenanceSchedule>
    {
        public MaintenanceScheduleRepository(string connstring) : base(connstring)
        {
        }

        public List<MaintenanceSchedule> GetAll()
        {
            using NpgsqlConnection conn = OpenConnection();
            return ExecuteSelect(conn,
                "SELECT id, batch_id, task_type, due_date, is_completed FROM maintenance_schedules ORDER BY due_date",
                MapRow);
        }

        public MaintenanceSchedule? GetById(string id)
        {
            return GetAll().FirstOrDefault(s => s.Id == id);
        }

        public List<MaintenanceSchedule> GetByBatch(string batchId)
        {
            using NpgsqlConnection conn = OpenConnection();
            return ExecuteSelect(conn,
                "SELECT * FROM st_select_schedules_by_batch(:_batch_id)",
                MapRow,
                p => p.AddWithValue("_batch_id", batchId));
        }

        public List<MaintenanceSchedule> GetDueOrOverdue(DateTime? referenceDate = null)
        {
            DateTime cutoff = (referenceDate ?? DateTime.Now).Date.AddDays(1).AddTicks(-1);
            return GetAll()
                .Where(s => !s.IsCompleted && s.DueDate <= cutoff)
                .OrderBy(s => s.DueDate)
                .ToList();
        }

        public List<MaintenanceSchedule> GetDueOrOverdueByUser(IEnumerable<CompostBatch> userBatches, DateTime? referenceDate = null)
        {
            DateTime cutoff = (referenceDate ?? DateTime.Now).Date.AddDays(1).AddTicks(-1);
            return userBatches
                .SelectMany(b => b.Schedules)
                .Where(s => !s.IsCompleted && s.DueDate <= cutoff)
                .OrderBy(s => s.DueDate)
                .ToList();
        }

        public void Add(MaintenanceSchedule entity)
        {
            ArgumentNullException.ThrowIfNull(entity);
            if (string.IsNullOrWhiteSpace(entity.BatchId))
            {
                throw new InvalidOperationException("BatchId pada MaintenanceSchedule wajib diisi.");
            }

            EnsureSuccess(ExecuteFunction(
                "SELECT st_insert_schedule(:_id, :_batch_id, :_task_type, :_due_date, :_is_completed)",
                p =>
                {
                    p.AddWithValue("_id", entity.Id);
                    p.AddWithValue("_batch_id", entity.BatchId);
                    p.AddWithValue("_task_type", entity.TaskType.ToString());
                    p.AddWithValue("_due_date", entity.DueDate);
                    p.AddWithValue("_is_completed", entity.IsCompleted);
                }), "Gagal menyimpan jadwal perawatan ke database.");
        }

        public void Update(MaintenanceSchedule entity)
        {
            ArgumentNullException.ThrowIfNull(entity);
            if (entity.IsCompleted)
            {
                CompleteSchedule(entity);
            }
        }

        public void Delete(string id)
        {
            EnsureSuccess(ExecuteFunction(
                "DELETE FROM maintenance_schedules WHERE id = :_id",
                p => p.AddWithValue("_id", id)),
                "Gagal menghapus jadwal perawatan dari database.");
        }

        public void CompleteSchedule(MaintenanceSchedule schedule)
        {
            ArgumentNullException.ThrowIfNull(schedule);

            EnsureSuccess(ExecuteFunction(
                "SELECT st_complete_schedule(:_id)",
                p => p.AddWithValue("_id", schedule.Id)),
                "Gagal memperbarui status jadwal perawatan di database.");
            schedule.MarkCompleted();
        }

        public void AddRecommendation(CompostRecommendation recommendation)
        {
            ArgumentNullException.ThrowIfNull(recommendation);

            EnsureSuccess(ExecuteFunction(
                "SELECT st_insert_recommendation(:_id, :_batch_id, :_message, :_priority, :_created_at)",
                p =>
                {
                    p.AddWithValue("_id", recommendation.Id);
                    p.AddWithValue("_batch_id", recommendation.BatchId);
                    p.AddWithValue("_message", recommendation.Message);
                    p.AddWithValue("_priority", recommendation.Priority);
                    p.AddWithValue("_created_at", recommendation.CreatedAt);
                }), "Gagal menyimpan riwayat rekomendasi ke database.");
        }

        public List<CompostRecommendation> GetRecommendationsByBatch(string batchId)
        {
            using NpgsqlConnection conn = OpenConnection();
            return ExecuteSelect(conn,
                "SELECT * FROM st_select_recommendations_by_batch(:_batch_id)",
                MapRecommendation,
                p => p.AddWithValue("_batch_id", batchId));
        }

        protected override MaintenanceSchedule MapRow(NpgsqlDataReader rd)
        {
            MaintenanceSchedule schedule = new MaintenanceSchedule(
                Enum.Parse<TaskType>((string)rd["task_type"]),
                (DateTime)rd["due_date"]);
            schedule.Id = (string)rd["id"];
            schedule.BatchId = (string)rd["batch_id"];
            schedule.IsCompleted = (bool)rd["is_completed"];
            return schedule;
        }

        private static CompostRecommendation MapRecommendation(NpgsqlDataReader rd)
        {
            CompostRecommendation rec = new CompostRecommendation(
                (string)rd["batch_id"],
                (string)rd["message"],
                (string)rd["priority"]);
            rec.Id = (string)rd["id"];
            rec.CreatedAt = (DateTime)rd["created_at"];
            return rec;
        }
    }
}