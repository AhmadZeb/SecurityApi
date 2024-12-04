namespace SecurityApi.Model
{
    public class Blog
    {
        public  int BlogId { get; set; }
        public string? Title { get; set; } // Corrected typo here
        public string? Summary { get; set; }
        public string? Description { get; set; } // Corrected typo here
        public string? FileName { get; set; }
        public DateTime? CreatedOn { get; set; } // Changed to DateTime for better date handling
        public string? CreatedBy { get; set; }
        public DateTime? ModifiedOn { get; set; } // Nullable DateTime for optional modification date
        public string? ModifiedBy { get; set; }


    }
}
