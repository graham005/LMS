using Microsoft.AspNetCore.Identity;

namespace LMS.Models.Data_Models
{
    public class Course
    {
        public int Id { get; set; }
        public string CourseName { get; set; }
        public string Description { get; set; }
        public string ImagePath { get; set; }
        public string TeacherId { get; set; }

        public IdentityUser Teacher { get; set; }
    }
}
