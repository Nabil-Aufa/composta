namespace Composta.Models
{
    public class UserBadge
    {
        private string _userId;
        private string _badgeId;
        private DateTime _earnedAt;

        public UserBadge()
        {
            _userId = string.Empty;
            _badgeId = string.Empty;
            _earnedAt = DateTime.Now;
        }

        public UserBadge(string userId, string badgeId) : this()
        {
            _userId = userId;
            _badgeId = badgeId;
        }

        public string UserId
        {
            get { return _userId; }
            set { _userId = value; }
        }

        public string BadgeId
        {
            get { return _badgeId; }
            set { _badgeId = value; }
        }

        public DateTime EarnedAt
        {
            get { return _earnedAt; }
            set { _earnedAt = value; }
        }
    }
}
