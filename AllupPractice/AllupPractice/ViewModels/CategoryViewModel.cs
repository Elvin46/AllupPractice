using AllupPractice.Models;
using System.Collections.Generic;

namespace AllupPractice.ViewModels
{
    public class CategoryViewModel
    {
        public Category SelectedCategory { get; set; }
        public List<Category> Categories { get; set; }
    }
}
