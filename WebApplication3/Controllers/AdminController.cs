using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using Quartz.Util;
using ReservationSystem.Models;
using ReservationSystem.Repository;
using ReservationSystem.Reservation;
using ReservationSystem.Utils;

namespace ReservationSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private IRepository _repository;
        private IReservationManager _reservationManager;
        public AdminController(IRepository repository, IReservationManager reservationManager)
        {
            this._repository = repository;
            this._reservationManager = reservationManager;
        }

        public ActionResult Reports(string sdate, string[] IsWeekly)
        {
            var model = new ReservationPrintable();

            model.IsWeekly = true;
            if (IsWeekly != null)
                model.IsWeekly = Boolean.Parse(IsWeekly[0]);

            var date = sdate == null ? DateTime.Today : DateTime.ParseExact(sdate, "dd.MM.yyyy", CultureInfo.InvariantCulture);
            if (model.IsWeekly)
            {
                var diff = date.DayOfWeek - CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek;
                if (diff < 0)
                    diff += 7;
                date = date.AddDays(-diff);
            }
            model.Date = date;

            using (IUnitOfWork uow = new UnitOfWork(new DbContextWrap()))
            {
                var days = model.IsWeekly ? 5 : 1;

                for (var i = 0; i < days; i++)
                {
                    var view = new ReservationView
                    {
                        Tables = _repository.GetAll<TableModel>(uow).ToList(),
                        Times = _repository.GetAll<TimeModel>(uow).ToList(),
                        Date = date.AddDays(i)
                    };
                    view.Day = _reservationManager.GetReservationsForDate(uow, view.Date, view.Tables, User.Identity.GetUserId());

                    var users = _reservationManager.GetUsersForDate(uow, view.Date);
                    model.AddUsers(HttpContext.GetOwinContext()
                                    .GetUserManager<ApplicationUserManager>()
                                    .Users
                                    .Where(u => users.Contains(u.Id))
                                    .Select(u => new MyUser { Id = u.Id, UserName = u.UserName, Name = u.Name})
                                    .ToList()
                                    .ToDictionary(u => u.Id));
                    model.AddDay(view);
                }
            }
            
            return View("Reports", model);
        }

        public ActionResult Settings()
        {
            var model = new SettingModelView();
            List<WeekDayModel> week = null;
            List<TimeModel> times = null;

            using (IUnitOfWork uow = new UnitOfWork(new DbContextWrap()))
            {
                model.Setting = _repository.GetAll<SettingModel>(uow).ToList();
                model.NumOfTables = _repository.GetAll<TableModel>(uow).OrderByDescending(item => item.Number).First().Number;
                week = _repository.GetAll<WeekDayModel>(uow).OrderBy(item => item.Id).ToList();
                times = _repository.GetAll<TimeModel>(uow).ToList();
                model.DateRanges = _repository.GetAll<DateRangeModel>(uow).ToList();
            }

            model.WeekDays = new List<WeekDaysView>();
            foreach (var dayView in week.Select(day => new WeekDaysView(day.Id, day.Name)
            {
                Times = times,
                StartTimeId = day.StartTime,
                StartTimeValue = times.Where(time => time.Id == day.StartTime).First().StartTime,
                EndTimeId = day.EndTime,
                EndTimeValue = times.Where(time => time.Id == day.EndTime).First().StartTime,
                IsCancelled = day.IsCancelled,
                DateRangeId = day.DateRange
            }))
            {
                model.WeekDays.Add(dayView);
            }

            using (var appContext = new ApplicationDbContext())
            {
                // Load the set of Admin user ids once, instead of calling IsInRole per user.
                // The old per-user IsInRole call caused one DB round-trip for every user (N+1),
                // which is what made this page crawl as the user list grew.
                var adminUserIds = new HashSet<string>();
                var adminRole = appContext.Roles.FirstOrDefault(r => r.Name == "Admin");
                if (adminRole != null)
                {
                    foreach (var userRole in adminRole.Users)
                    {
                        adminUserIds.Add(userRole.UserId);
                    }
                }

                model.Users = appContext.Users
                    .Select(u => new { u.Id, u.Email, u.UserName, u.Name, u.FirstName, u.LastName, u.PhoneNumber, u.IsActive })
                    .ToList()
                    .Select(u => new MyUser
                    {
                        Id = u.Id,
                        Email = u.Email,
                        UserName = u.UserName,
                        Name = u.Name,
                        FirstName = u.FirstName,
                        LastName = u.LastName,
                        PhoneNumber = u.PhoneNumber,
                        IsActive = u.IsActive,
                        IsAdmin = adminUserIds.Contains(u.Id)
                    })
                    .ToList();
            }

            return View("Settings", model);
        }

        [HttpGet]
        public ActionResult Edit(int? id)
        {
            SettingModel model = null;
            using (IUnitOfWork uow = new UnitOfWork(new DbContextWrap()))
            {
                model = _repository.GetByKey<SettingModel>(uow, id);
            }
            return PartialView("_EditPartialView", model);
        }

        [HttpPost] // this action takes the viewModel from the modal
        public ActionResult Edit(SettingModel model, string svalue)
        {
            using (IUnitOfWork uow = new UnitOfWork(new DbContextWrap()))
            {
                var m = _repository.GetByKey<SettingModel>(uow, model.Id);
                m.Value = svalue;
                _repository.Update(uow,m);
                uow.SaveChanges();
            }

            return RedirectToAction("Settings");
        }

        [HttpGet]
        public ActionResult EditWeekDay(int? id)
        {
            WeekDayModel model = null;
            List<TimeModel> times = null;

            using (IUnitOfWork uow = new UnitOfWork(new DbContextWrap()))
            {
                model = _repository.GetByKey<WeekDayModel>(uow, id);
                times = _repository.GetAll<TimeModel>(uow).ToList();
            }

            var view = new WeekDaysView(model.Id, model.Name)
            {
                StartTimeValue = times.Where(x => x.Id == model.StartTime).First().StartTime,
                EndTimeValue = times.Where(x => x.Id == model.EndTime).First().StartTime,
                IsCancelled = model.IsCancelled,
                Times = times
            };

            return PartialView("_EditWeekDayPartialView", view);
        }

        [HttpPost] // this action takes the viewModel from the modal
        public ActionResult EditWeekDay(WeekDayModel model, string startTime, string endTime, bool cancelledDay)
        {
            using (IUnitOfWork uow = new UnitOfWork(new DbContextWrap()))
            {
                var m = _repository.GetByKey<WeekDayModel>(uow, model.Id);
                var times = _repository.GetAll<TimeModel>(uow);

                if (!string.IsNullOrEmpty(startTime))
                {
                    m.StartTime = times.First(x => x.StartTime.ToString().Equals(startTime)).Id;
                }

                if (!string.IsNullOrEmpty(endTime))
                {
                    m.EndTime = times.First(x => x.StartTime.ToString().Equals(endTime)).Id;
                }

                m.IsCancelled = cancelledDay;
                _repository.Update(uow, m);
                uow.SaveChanges();
            }

            return RedirectToAction("Settings");
        }


        public ActionResult ChangeAdmin(string id, bool? WasAdmin)
        {
            using (var appContext = new ApplicationDbContext())
            {
                var userStore = new UserStore<ApplicationUser>(appContext);
                var userManager = new UserManager<ApplicationUser>(userStore);
                if (WasAdmin.Value)
                    userManager.RemoveFromRole(id, "Admin");
                else
                    userManager.AddToRole(id, "Admin");

                appContext.SaveChanges();
            }
            return RedirectToAction("Settings");
        }

        public ActionResult ChangeActive(string id, bool wasActive)
        {
            // Enable/disable a user account (item 2). Inactive users cannot log in and show up
            // as pending in the admin settings page until an admin activates them here.
            using (var appContext = new ApplicationDbContext())
            {
                var user = appContext.Users.FirstOrDefault(u => u.Id == id);
                if (user != null)
                {
                    user.IsActive = !wasActive;
                    appContext.SaveChanges();
                }
            }
            return RedirectToAction("Settings");
        }

        public ActionResult CancelReservation()
        {
            var view = new List<EditReservationView>();
            using (IUnitOfWork uow = new UnitOfWork(new DbContextWrap()))
            {
                var reservations = this._reservationManager.GetReservations(uow);
                Dictionary<string, string> users = null;
                using (var appContext = new ApplicationDbContext())
                {
                    users = HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>().Users
                        .Select(u => new { u.Id, u.Name })
                        .ToDictionary(t => t.Id, t => t.Name);
                }
                view.AddRange(reservations.Select(res => new EditReservationView()
                { Id = res.Id, Name = users.ContainsKey(res.UserId) ? users[res.UserId] : res.UserId, Date = res.Date, Table = res.Table.Number, Time = res.Time.StartTime })
                .OrderBy(x => x.Date)
                .ThenBy(x => x.Table)
                .ThenBy(x => x.Time));
            }
            return View("CancellReservations", view);
        }

        public ActionResult Delete(int? id)
        {
            using (var uow = new UnitOfWork(new DbContextWrap()))
            {
                var reservationModel = _repository.Get<ReservationModel, int>(uow, (r => r.Id == id), (r => r.Id)).FirstOrDefault();

                if (reservationModel != null)
                {
                    _repository.Delete<ReservationModel>(uow, reservationModel);
                    uow.SaveChanges();
                }
            }

            return RedirectToAction("CancelReservation", "Admin");
        }

        public ActionResult DeleteTable()
        {
            using (var uow = new UnitOfWork(new DbContextWrap()))
            {
                var model = _repository.GetAll<TableModel>(uow).OrderByDescending(item => item.Number).First();
                _repository.Delete(uow, model);
                uow.SaveChanges();
            }


            return RedirectToAction("Settings", "Admin");
        }

        public ActionResult CreateTable()
        {
            using (var uow = new UnitOfWork(new DbContextWrap()))
            {
                var number = _repository.GetAll<TableModel>(uow).OrderByDescending(item => item.Number).First().Number;
                number++;
                var model = new TableModel(number);

               
                _repository.Add<TableModel>(uow, model);

                uow.SaveChanges();
            }


            return RedirectToAction("Settings", "Admin");

        }

        public ActionResult DeleteDateRange(int id)
        {
            using (var uow = new UnitOfWork(new DbContextWrap()))
            {
                var days = _repository.GetAll<WeekDayModel>(uow).Where(d => d.DateRange == id);
                foreach (var day in days)
                {
                    _repository.Delete(uow, day);
                }


                var model = _repository.GetByKey<DateRangeModel>(uow, id);
                _repository.Delete(uow, model);
                uow.SaveChanges();
            }

            return RedirectToAction("Settings", "Admin");
        }

        [HttpGet]
        public ActionResult EditDateRange(int id)
        {
            DateRangeModel model = null;

            using (IUnitOfWork uow = new UnitOfWork(new DbContextWrap()))
            {
                model = _repository.GetByKey<DateRangeModel>(uow, id);
            }

            return PartialView("_EditDateRangePartialView", model);
        }

        [HttpPost] // this action takes the viewModel from the modal
        public ActionResult EditDateRange(DateRangeModel model, string name, string startDate, string endDate)
        {
            using (IUnitOfWork uow = new UnitOfWork(new DbContextWrap()))
            {
                var m = _repository.GetByKey<DateRangeModel>(uow, model.Id);

                if (!string.IsNullOrEmpty(name) && !name.Equals(m.Name))
                {
                    m.Name = name;
                }

                if (!string.IsNullOrEmpty(startDate))
                {
                    m.StartDate = DateTime.ParseExact(startDate, "dd.MM", CultureInfo.InvariantCulture);
                }

                if (!string.IsNullOrEmpty(endDate))
                {
                    m.EndTime = DateTime.ParseExact(endDate, "dd.MM", CultureInfo.InvariantCulture);
                }

                _repository.Update(uow, m);
                uow.SaveChanges();
            }

            return RedirectToAction("Settings");
        }

        [HttpGet]
        public ActionResult CreateDateRange()
        {
            return PartialView("_CreateDateRangePartialView");
        }

        [HttpPost]
        public ActionResult CreateDateRange(string name, string startDate, string endDate)
        {
            DateRangeModel dateRange = null;
            try
            {
                dateRange = new DateRangeModel()
                {
                    Name = name,
                    StartDate = DateTime.ParseExact(startDate, "dd.MM", CultureInfo.InvariantCulture),
                    EndTime = DateTime.ParseExact(endDate, "dd.MM", CultureInfo.InvariantCulture),
                    IsActive = false
                };
            }
            catch (Exception)
            {
               return RedirectToAction("MainTable", "Home", new { code = new ReturnCode(ReturnCodeLevel.ERROR, Resource.WrongDateFormat, "").ToString() });
            }

            using (IUnitOfWork uow = new UnitOfWork(new DbContextWrap()))
            {
                var ranges = _repository.GetAll<DateRangeModel>(uow);
                
                if(ranges.Any(dr => dr.IsIn(dateRange.StartDate) || dr.IsIn(dateRange.EndTime)))
                    return RedirectToAction("MainTable", "Home", new { code = new ReturnCode(ReturnCodeLevel.WARNING, Resource.RangesCollision, "").ToString() });

                _repository.Add(uow, dateRange);
                uow.SaveChanges();

                _repository.Add(uow, dateRange);
                uow.SaveChanges();

                // Id is populated on the tracked entity after SaveChanges - no need to re-query by name.
                var dateRangeId = dateRange.Id;

                // Load the times once instead of scanning the TimeModel table five separate times.
                var times = _repository.GetAll<TimeModel>(uow).ToList();
                var t1800 = times.First(item => item.StartTime == new TimeSpan(18, 0, 0));
                var t2100 = times.First(item => item.StartTime == new TimeSpan(21, 0, 0));
                var t2000 = times.First(item => item.StartTime == new TimeSpan(20, 0, 0));
                var t1830 = times.First(item => item.StartTime == new TimeSpan(18, 30, 0));
                var t2130 = times.First(item => item.StartTime == new TimeSpan(21, 30, 0));

                MakeDaysOfWeekForDateRange(uow, dateRangeId, t1800, t2100, t2000, t1830, t2130);

                uow.SaveChanges();
            }

            return RedirectToAction("Settings");
        }

        private void MakeDaysOfWeekForDateRange(IUnitOfWork uow, int dateRangeId, TimeModel t1800, TimeModel t2100, TimeModel t2000, TimeModel t1830, TimeModel t2130)
        {
            var monday = new WeekDayModel()
            {
                Name = DayOfWeek.Monday.ToString(),
                StartTimeKey = t1800,
                StartTime = t1800.Id,
                EndTimeKey = t2100,
                EndTime = t2100.Id,
                IsCancelled = false,
                DateRange = dateRangeId
            };

            var tuesday = new WeekDayModel()
            {
                Name = DayOfWeek.Tuesday.ToString(),
                StartTimeKey = t1800,
                StartTime = t1800.Id,
                EndTimeKey = t2100,
                EndTime = t2100.Id,
                IsCancelled = false,
                DateRange = dateRangeId
            };

            var wednesday = new WeekDayModel()
            {
                Name = DayOfWeek.Wednesday.ToString(),
                StartTimeKey = t1800,
                StartTime = t1800.Id,
                EndTimeKey = t2000,
                EndTime = t2000.Id,
                IsCancelled = false,
                DateRange = dateRangeId
            };

            var thursday = new WeekDayModel()
            {
                Name = DayOfWeek.Thursday.ToString(),
                StartTimeKey = t1830,
                StartTime = t1830.Id,
                EndTimeKey = t2130,
                EndTime = t2130.Id,
                IsCancelled = false,
                DateRange = dateRangeId
            };

            var friday = new WeekDayModel()
            {
                Name = DayOfWeek.Friday.ToString(),
                StartTimeKey = t1800,
                StartTime = t1800.Id,
                EndTimeKey = t2100,
                EndTime = t2100.Id,
                IsCancelled = true,
                DateRange = dateRangeId
            };

            _repository.Add(uow, monday);
            _repository.Add(uow, tuesday);
            _repository.Add(uow, wednesday);
            _repository.Add(uow, thursday);
            _repository.Add(uow, friday);
        }
    }
}
