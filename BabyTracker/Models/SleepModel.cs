namespace BabyTracker.Models
{
    public class SleepModel : EntryModel
    {
        public string Duration { get; set; }

        public string DurationText 
        {
            get 
            {
                var success = int.TryParse(Duration, out var durationInMins);

                if (!success) 
                {
                    return string.Empty;
                }

                if (durationInMins < 60) 
                {
                    return $"{durationInMins} min";
                }
                
                return $"{(durationInMins / 60)} hrs {durationInMins % 60} min";
            }
        }
    }
}
