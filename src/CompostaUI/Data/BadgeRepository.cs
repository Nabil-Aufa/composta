using Composta.Models;
using Npgsql;

namespace Composta.Data
{
    public class BadgeRepository : PostgresRepository<UserBadgeStatus>
    {
        public BadgeRepository(string connstring) : base(connstring)
        {
        }

        public List<UserBadgeStatus> GetBadgesByUser(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return new List<UserBadgeStatus>();
            }

            return ExecuteSelect("SELECT * FROM st_select_badges_by_user(:_user_id)",
                p => p.AddWithValue("_user_id", userId));
        }

        public bool AwardBadge(UserBadge userBadge)
        {
            ArgumentNullException.ThrowIfNull(userBadge);

            int inserted = ExecuteFunction("SELECT st_insert_user_badge(:_user_id, :_badge_id, :_earned_at)",
                p =>
                {
                    p.AddWithValue("_user_id", userBadge.UserId);
                    p.AddWithValue("_badge_id", userBadge.BadgeId);
                    p.AddWithValue("_earned_at", userBadge.EarnedAt);
                });

            return inserted == 1;
        }

        public List<Badge> EvaluateAndAwardBadges(User user)
        {
            ArgumentNullException.ThrowIfNull(user);

            List<UserBadgeStatus> statuses = GetBadgesByUser(user.Id);
            List<Badge> newlyUnlocked = new List<Badge>();

            foreach (UserBadgeStatus status in statuses)
            {
                if (status.IsEarned || !status.Badge.IsUnlocked(user))
                {
                    continue;
                }

                UserBadge award = new UserBadge(user.Id, status.Badge.Id);
                if (AwardBadge(award))
                {
                    newlyUnlocked.Add(status.Badge);
                }
            }

            return newlyUnlocked;
        }

        protected override UserBadgeStatus MapRow(NpgsqlDataReader rd)
        {
            Badge badge = new Badge(
                (string)rd["name"],
                (string)rd["criteria"],
                rd["icon_path"] as string ?? string.Empty);
            badge.Id = (string)rd["id"];

            DateTime? earnedAt = rd["earned_at"] is DBNull ? null : (DateTime)rd["earned_at"];
            return new UserBadgeStatus(badge, earnedAt);
        }
    }
}
