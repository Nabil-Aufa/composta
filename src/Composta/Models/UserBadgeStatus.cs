namespace Composta.Models
{
    public class UserBadgeStatus
    {
        public UserBadgeStatus(Badge badge, DateTime? earnedAt)
        {
            Badge = badge ?? throw new ArgumentNullException(nameof(badge));
            EarnedAt = earnedAt;
        }

        public Badge Badge { get; }
        public DateTime? EarnedAt { get; }
        public bool IsEarned => EarnedAt.HasValue;
    }
}
