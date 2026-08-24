using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using ReservationSystem.Models;

namespace ReservationSystem.Utils
{
    public class MyUser
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public string UserName { get; set; }

        public string Email { get; set; }

        public string PhoneNumber { get; set; }

        public bool IsActive { get; set; }

        public bool IsAdmin { get; set; }

        public static string GetNameOfTheUser(string userId)
        {
            using (var appContext = new ApplicationDbContext())
            {
                var userStore = new UserStore<ApplicationUser>(appContext);
                var userManager = new UserManager<ApplicationUser>(userStore);
                var user = userManager.FindById(userId);
                return user.Name;
            }
        }
    }
}