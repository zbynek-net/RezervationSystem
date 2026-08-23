using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Runtime.Versioning;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.Security;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using ReservationSystem.Models;
using ReservationSystem.Repository;
using ReservationSystem.Reservation;
using ReservationSystem.Utils;
using log4net;

namespace ReservationSystem.Controllers
{
    [Authorize]
    public class ReservationController : Controller
    {
        private IRepository repository;
        private IReservationManager reservationManager;
        private EmailController emailController;
        readonly ILog logger = LogManager.GetLogger(typeof(ReservationController));

        public ReservationController(IReservationManager reservationManager, IRepository repository, EmailController emailController)
        {
            this.repository = repository;
            this.reservationManager = reservationManager;
            this.emailController = emailController;
        }

        // GET: Reservation
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Save(DateTime? sdate)
        {
            try
            {
                var date = sdate.Value;
                using (IUnitOfWork uow = new UnitOfWork(new DbContextWrap()))
                {
                    var picked = reservationManager.GetPickedForDateAndUser(uow, date, User.Identity.GetUserId());
                    if (picked.Count == 0)
                    {
                        return RedirectToAction("MainTable", "Home", new { code = new ReturnCode(ReturnCodeLevel.WARNING, Resource.ReservationExpired, null).ToString(), date = date });
                    }

                    var emailsData = new List<KeyValuePair<string, TimeSpan>>();
                    foreach (var pick in picked)
                    {
                        var model = new ReservationModel()
                        {
                            Date = pick.PickedDate,
                            TableId = pick.TableId,
                            TimeId = pick.TimeId,
                            UserId = pick.UserId
                        };
                        repository.Delete<PickedModel>(uow, pick);
                        repository.Add<ReservationModel>(uow, model);
                        
                        var table =
                            repository.Get<TableModel, int>(uow, item => item.Id == pick.TableId, item => item.Id)
                                .First().Number.ToString();
                        var time = repository.Get<TimeModel, int>(uow, item => item.Id == pick.TimeId, item => item.Id)
                                .First().StartTime;
                        var data = new KeyValuePair<string, TimeSpan>(table, time);
                        emailsData.Add(data);

                    }
                    emailController.SendReservationConfirmation(User.Identity.GetUserName(), emailsData, sdate.Value.ToString("MM/dd/yyyy"));
                    uow.SaveChanges();
                }
                
                return RedirectToAction("MainTable", "Home", new { code = new ReturnCode(ReturnCodeLevel.SUCCESS, Resource.ReservationSuccess, Resource.ReservationSuccessReason).ToString(), date = date });
            }
            catch(Exception ex)
            {
                logger.Error(ex.Message + Environment.NewLine + ex.StackTrace);
                return RedirectToAction("MainTable", "Home", new { code = new ReturnCode(ReturnCodeLevel.ERROR, Resource.WriteAnAdministrator, ex.Message).ToString()});
            }
        }

        public ActionResult PickedTime(int table, int time, DateTime? sdate)
        {
            DateTime date = sdate.Value;
            string userId = User.Identity.GetUserId();

            using (IUnitOfWork uow = new UnitOfWork(new DbContextWrap()))
            {
                if (this.CheckPeriod(uow, table, time, date, userId))
                {
                    return RedirectToAction("MainTable", "Home", new { code = new ReturnCode(ReturnCodeLevel.WARNING, Resource.TableAlreadyPickedWarning, null).ToString(), date = date });
                }

                //is not after deadline
                if (reservationManager.IsAfterDeadline(uow,sdate.Value) && !User.IsInRole("Admin"))
                {
                    return RedirectToAction("MainTable", "Home", new { code = new ReturnCode(ReturnCodeLevel.WARNING, Resource.AfterDeadlineWarning, null).ToString(), date = date });
                }

                var userPickeds = repository.Get<PickedModel, int>(uow,
                    (pick => pick.TableId == table && pick.TimeId == time && pick.UserId == userId),
                    (item => item.Id));
                var pickedModels= userPickeds as IList<PickedModel> ?? userPickeds.Where(pick => pick.PickedDate.Date == date.Date).ToList();
                if (pickedModels.Any())
                {
                    //unpicked
                    foreach (var picked in pickedModels)
                        repository.Delete<PickedModel>(uow, picked);
                }
                else
                {
                    //picked
                    var picked = new PickedModel()
                    {
                        UserId = userId,
                        TableId = table,
                        TimeId = time,
                        PickedDate = date,
                        TimeStamp = DateTime.Now
                    };
                    repository.Add<PickedModel>(uow, picked);
                }

                uow.SaveChanges();
            }

            if (table == 1 || table == 2 || table == 3 || table == 4)
                return RedirectToAction("MainTable", "Home", new { code = new ReturnCode(ReturnCodeLevel.WARNING, Resource.TablesOneFour, null).ToString(), date = date });
            
            return RedirectToAction("MainTable", "Home", new {code= new ReturnCode(ReturnCodeLevel.RELOAD, Resource.ReloadOK, null).ToString(), date =date});
        }

        private bool CheckPeriod(IUnitOfWork uow, int table, int time, DateTime date, string userId)
        {
            var pickedModels = this.GetPicksBy(uow, table, time, date, userId);

            var changes = false;
            foreach (var model in pickedModels)
            {
                if (DateTime.Now > model.TimeStamp.AddMinutes(15))
                {
                    this.repository.Delete<PickedModel>(uow, model);
                    changes = true;
                }
            }

            if (changes)
            {
                uow.SaveChanges();
                pickedModels = this.GetPicksBy(uow, table, time, date, userId);
            }

            return pickedModels.Any();
        }

        private IEnumerable<PickedModel> GetPicksBy(IUnitOfWork uow, int table, int time, DateTime date, string userId)
        {
            return repository.Get<PickedModel, int>(
                uow,
                (pick =>
                    pick.TableId == table &&
                    pick.TimeId == time &&
                    pick.PickedDate == date.Date &&
                    pick.UserId != userId),
                (item => item.Id));
        }

        public ActionResult GroupReservations(string date, string reservationName, string startTime, string endTime, IEnumerable<string> tables)
        {
            if (date == null && startTime == null)
            {
                var view = new ReservationView();
                using (var uow = new UnitOfWork(new DbContextWrap()))
                {
                    view.Tables = repository.GetAll<TableModel>(uow).ToList();
                    view.Times = repository.GetAll<TimeModel>(uow).ToList();
                }
                return View("GroupReservations", view);
            }
            else
            {
                var timeIds = DateUtil.GetTimeIds(repository, startTime, endTime);
                var models = new List<ReservationModel>();
                var dates = date.Split(',');

                foreach (var sdate in dates)
                {
                    var finalDate = DateTime.ParseExact(sdate.Replace(" ", ""), "dd.MM.yyyy", CultureInfo.InvariantCulture);
                    foreach (var table in tables)
                    {
                        //pro kazdy stul
                        models.AddRange(timeIds.Select(time => new ReservationModel()
                        {
                            Name = reservationName,
                            Date = finalDate.Date,
                            TableId = Int32.Parse(table),
                            TimeId = time,
                            UserId = User.Identity.GetUserId()
                        }));
                    }
                }

                using (IUnitOfWork uow = new UnitOfWork(new DbContextWrap()))
                {
                    foreach (var model in models)
                    {
                        this.repository.Add<ReservationModel>(uow, model);
                    }
                    uow.SaveChanges();
                }
               
                return RedirectToAction("MainTable", "Home", new { code = new ReturnCode(ReturnCodeLevel.SUCCESS, Resource.ReservationSuccess, Resource.GroupReservationSuccessReason).ToString(), date = date });
            }
        }

        public ActionResult EditReservations()
        {
            var view = new List<EditReservationView>();
            using (IUnitOfWork uow = new UnitOfWork(new DbContextWrap()))
            {
                var reservations = this.reservationManager.GetReservationsForUser(uow, User.Identity.GetUserId());
                view.AddRange(reservations.Select(res => new EditReservationView() {Id = res.Id, Date = res.Date, Table = res.Table.Number, Time = res.Time.StartTime}));
            }
            return View("EditReservations", view);
        }

        public ActionResult Delete(int? id)
        {
            using (var uow = new UnitOfWork(new DbContextWrap()))
            {


                var reservationModel = repository.Get<ReservationModel, int>(uow, (r => r.Id == id), (r => r.Id)).FirstOrDefault();
                if (reservationModel == null)
                {
                    return RedirectToAction("MainTable", "Home", new { code = new ReturnCode(ReturnCodeLevel.RELOAD, Resource.ReloadOK, null).ToString() });
                }
                //is not after deadline
                if (reservationManager.IsAfterDeadline(uow, reservationModel.Date))
                {
                    return RedirectToAction("MainTable", "Home", new { code = new ReturnCode(ReturnCodeLevel.WARNING, Resource.AfterDeadlineWarning, null).ToString(), date = reservationModel.Date });
                }
                repository.Delete<ReservationModel>(uow, reservationModel);
                uow.SaveChanges();
            }

            return RedirectToAction("MainTable", "Home", new { code = new ReturnCode(ReturnCodeLevel.SUCCESS, Resource.ReservationDeleteSucess, null).ToString()});
        }

        public ActionResult CancelledDay(string date, string reason)
        {
            var dates = date.Split(',');

            var cancellList = dates.Select(sdate => DateTime.ParseExact(sdate.Replace(" ", ""), "dd.MM.yyyy", CultureInfo.InvariantCulture)).Select(finalDate => new CancelledDayModel()
            {
                Date = finalDate, Reason = reason
            }).ToList();

            using (IUnitOfWork uow = new UnitOfWork(new DbContextWrap()))
            {
                foreach (var cancel in cancellList)
                {
                    repository.Add<CancelledDayModel>(uow, cancel);
                }
                uow.SaveChanges();
            }
            return RedirectToAction("MainTable", "Home", new { code = new ReturnCode(ReturnCodeLevel.SUCCESS, Resource.CancellationOk, null).ToString(), date = date });

        }
    }
}