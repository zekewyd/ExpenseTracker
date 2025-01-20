using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace ExpenseTracker.Controllers
{
    public class calendarEvent
    {
        public string Summary { get; set; }
        public string Description { get; set; }
        public string PickedDate { get; set; }
    }

    public class Calendar : Controller
    {
        public List<calendarEvent> GoogleEvents = new List<calendarEvent>();
        static string[] Scopes = { CalendarService.Scope.Calendar, CalendarService.Scope.CalendarEvents };
        static string ApplicationName = "Expense Tracker";
        private const string SecondaryCalendarFile = "secondary_calendar_id.txt"; // To store the secondary calendar ID

        public ActionResult Index()
        {
            // ensure secondary calendar exists and get its ID
            string secondaryCalendarId = GetOrCreateSecondaryCalendar();

            // fetch events from the secondary calendar
            CalendarEvents(secondaryCalendarId);

            ViewBag.EventList = GoogleEvents;
            return View();
        }

        private string GetOrCreateSecondaryCalendar()
        {
            UserCredential credential;
            string path = Path.Combine(Directory.GetCurrentDirectory(), "credentials.json");
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                string credPath = "token.json";
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
            }

            var service = new CalendarService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });

            // check if the secondary calendar ID is stored
            if (System.IO.File.Exists(SecondaryCalendarFile))
            {
                return System.IO.File.ReadAllText(SecondaryCalendarFile);
            }

            // create a new secondary calendar
            Google.Apis.Calendar.v3.Data.Calendar newCalendar = new Google.Apis.Calendar.v3.Data.Calendar
            {
                Summary = "Expenses",
                Description = "Calendar for tracking expenses",
                TimeZone = "UTC"
            };

            Google.Apis.Calendar.v3.Data.Calendar createdCalendar = service.Calendars.Insert(newCalendar).Execute();

            // save the secondary calendar ID for future use
            System.IO.File.WriteAllText(SecondaryCalendarFile, createdCalendar.Id);

            return createdCalendar.Id;
        }

        public void CalendarEvents(string calendarId)
        {
            UserCredential credential;
            string path = Path.Combine(Directory.GetCurrentDirectory(), "credentials.json");
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                string credPath = "token.json";
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
            }

            var service = new CalendarService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });

            // define request parameters for the secondary calendar
            EventsResource.ListRequest request = service.Events.List(calendarId);
            request.TimeMin = DateTime.Now; // Fetch events starting from the current date
            request.ShowDeleted = false;
            request.SingleEvents = true;
            request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

            // list events
            Events events = request.Execute();
            if (events.Items != null && events.Items.Count > 0)
            {
                foreach (var eventItem in events.Items)
                {
                    var calendarevent = new calendarEvent
                    {
                        Summary = eventItem.Summary,
                        PickedDate = eventItem.Start.Date ?? DateTime.Now.ToString("yyyy-MM-dd"),
                        Description = eventItem.Description
                    };
                    GoogleEvents.Add(calendarevent);
                }
            }
        }
    }
}