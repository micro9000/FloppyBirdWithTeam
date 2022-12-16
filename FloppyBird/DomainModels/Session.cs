namespace FloppyBird.DomainModels
{
    public class Session
    {
        public Guid SessionToken { get; set; }
        public string Name { get; set; }
        public bool IsStarted { get; set; }
        public bool IsEnded { get; set; }

        public string Status {
			      get { 
                if (!IsStarted && !IsEnded)
                {
                    return "Waiting...";
                }

				        if (IsStarted && !IsEnded)
				        {
					        return "Started";
				        }

				        if (IsStarted && IsEnded)
				        {
					        return "Finished";
				        }

				        return "Unknown";
            }
		    }

        public DateTime StartedAt { get; set; }
        public Guid GameMasterAccountToken { get; set; }
        public List<User> Users { get; set; } = new List<User>();
    }
}
