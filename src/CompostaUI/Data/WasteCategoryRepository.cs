using Composta.Models;
using Npgsql;

namespace Composta.Data
{
    public class WasteCategoryRepository : PostgresRepository<WasteCategory>
    {
        public WasteCategoryRepository(string connstring) : base(connstring)
        {
        }

        public List<WasteCategory> GetAll()
        {
            return ExecuteSelect("SELECT * FROM st_select_categories()");
        }

        protected override WasteCategory MapRow(NpgsqlDataReader rd)
        {
            WasteCategory category = new WasteCategory((string)rd["name"],
                Convert.ToDouble(rd["co2e_factor_per_kg"]), (bool)rd["is_compostable"]);
            category.Id = (string)rd["id"];
            return category;
        }
    }
}
