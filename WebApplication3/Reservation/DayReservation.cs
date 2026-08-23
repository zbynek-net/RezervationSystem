using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using ReservationSystem.Models;

namespace ReservationSystem.Reservation
{
    public class DayReservation
    {
        private Dictionary<int,TableReservation> reservations;

        private bool _isCancelled;

        public DayReservation()
        {
            reservations = new Dictionary<int, TableReservation>();
            _isCancelled = false;
        }

        public DayReservation(bool isCancelled)
        {
            reservations = new Dictionary<int, TableReservation>();
            _isCancelled = isCancelled;
        }

        public bool IsCancelled { get { return this._isCancelled; } }

        public void Add(TableReservation tr)
        {
            this.reservations.Add(tr.TableNumber, tr);
        }

        public int IsReserved(TimeModel time, TableModel table)
        {
            var tableReservation = reservations[table.Number];
            if (tableReservation.IsReservation(time.Id))
                return 3;
            else if(tableReservation.IsPicked(time.Id))
                return 2;
            else
                return 1;
        }

        public string UserFromReservation(TimeModel time, TableModel table)
        {
            if (reservations.Count > 0)
            {
                var tableReservation = reservations[table.Number];
                return tableReservation.GetUser(time.Id);
            }
            return string.Empty;
        }
    }
}