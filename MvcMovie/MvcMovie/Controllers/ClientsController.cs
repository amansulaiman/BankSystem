﻿using System;
using System.Activities;
using System.Activities.Expressions;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.DynamicData;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.Services.Description;
using System.Web.UI.WebControls;
using Microsoft.Ajax.Utilities;
using MvcMovie.Models;

namespace MvcMovie.Controllers
{
    public class ClientsController : Controller
    {
        public static ClientsDbContext DataBase = new ClientsDbContext(); 

        public ActionResult Index(RouteValueDictionary rvd)
        {
            object x;
            rvd.TryGetValue("id", out x);
            
            Client client = new Client();
            if (x != null)
            {
                client = DataBase.Clients.Find(int.Parse(x.ToString()));
            }

            return View(client);
        }

        //-------------------------------------------------------------------------------------------------

        public ActionResult LogIn()
        {
            Client client = new Client(); 
            return View(client);
        }

        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LogIn(Client client)
        {
            if (ModelState.IsValid)
            {
                if (client.FirstName == "a" &&
                    client.SecondName == "a" &&
                    client.Password == "a")
                {
                    return RedirectToAction("Index", "Admin");
                }
                
                var c = client;
                var clients = from d in DataBase.Clients
                         where d.FirstName == c.FirstName &&
                         d.SecondName == c.SecondName &&
                         d.Password == c.Password
                         select d;

                if (clients.Count() == 1)
                {
                    client = clients.First();

                    RouteValueDictionary rvd = new RouteValueDictionary(client);

                    Session["client"] = client;

                    return  RedirectToAction("Index", "Clients", rvd);
                }

                ModelState.AddModelError("FirstName", "Field may be wrong");
                ModelState.AddModelError("SecondName", "Field may be wrong");
                ModelState.AddModelError("Password", "Field may be wrong");
            }

            return View(client);
        }

        //-------------------------------------------------------------------------------------------------

        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            } 

            Client client = DataBase.Clients.Find(id);
            if (client == null)
            {
                return HttpNotFound();
            }
            return View(client);
        }

        //-------------------------------------------------------------------------------------------------

        public ActionResult Create()
        {
            Client client = new Client();
            
            return View(client);
        }

        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Client client)
        {
            if (ModelState.IsValid)
            {
                List<Client> list = DataBase.Clients.ToList();
                var bankClient = list.Any(p => String.CompareOrdinal(p.Email, client.Email) == 0);
                
                if (bankClient)
                {
                    ModelState.AddModelError("Email", "Such email is already used");
                    return View(client);
                }
                
                DataBase.Clients.Add(client);
                DataBase.SaveChanges();

                RouteValueDictionary rvd = new RouteValueDictionary(client);
                    
                return RedirectToAction("Index", "Clients", rvd);
            }

            return View(client);
        }

        //-------------------------------------------------------------------------------------------------
        
        public ActionResult Edit(int id = 0)
        {
            Client client = DataBase.Clients.Find(id);
            if (client == null)
            {
                return HttpNotFound();
            }
            
            return View(client);
        }

        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(FormCollection client)
        {
            if (ModelState.IsValid)
            {
                int id = int.Parse(client["id"]);

                Client currClient = DataBase.Clients.ToList().First(c => c.ID == id);

                List<Client> list = DataBase.Clients.ToList();
                var bankClient = list.Any(p => String.CompareOrdinal(p.Email, client["Email"]) == 0 && p.ID != id);

                if (bankClient)
                {
                    ModelState.AddModelError("Email", "Such email is already used");
                    return View("Edit", currClient);
                }


                if (currClient != null)
                {
                    currClient.FirstName = client["FirstName"];
                    currClient.SecondName = client["SecondName"];
                    currClient.Email = client["Email"];
                    currClient.Password = client["Password"];
                    currClient.AccountType = client["AccountType"];
                }

                DataBase.Entry(currClient).State = EntityState.Modified;
                DataBase.SaveChanges();

                return RedirectToAction("Index", "Clients", new RouteValueDictionary(currClient));
            }

            return View(DataBase.Clients.Find(client));
        }

        //-------------------------------------------------------------------------------------------------

        public ActionResult Delete(int id = 0)
        {
            Client client = DataBase.Clients.Find(id);
            
            if (client == null)
            {
                return HttpNotFound();
            }

            return View(client);
        }

        
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Client client = DataBase.Clients.Find(id);
            
            DataBase.Clients.Remove(client);
            DataBase.SaveChanges();
            
            return RedirectToAction("LogIn", "Clients");
        }

        //-------------------------------------------------------------------------------------------------

        protected override void Dispose(bool disposing)
        {
            //DataBase.Dispose();
            //base.Dispose(disposing);
        }

        //-------------------------------------------------------------------------------------------------

        public ActionResult Operations(Client client)
        {
            return View();
        }

        public ActionResult PutMoney()
        {
            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult PutMoney(FormCollection form)
        {
            string bankID = form["BankID"];
            int sum;

            try
            {
                sum = int.Parse(form["Sum"]);
            }
            catch (FormatException)
            {
                ModelState.AddModelError("Sum", "Entered wrong sum");
                return View();
            }
 
            List<Client> list = DataBase.Clients.ToList();

            var bankClient = list.FirstOrDefault(p => String.CompareOrdinal(p.State.BankID, bankID) == 0);

            if (bankClient == null)
            {
                ModelState.AddModelError("BankID", "No such ID");
                return View();
            }

            if (sum <= 0 || sum > 100000000)
            {
                ModelState.AddModelError("Sum", "Entered wrong sum");
                return View();
            }

            MoneyOperation moneyOperation = new MoneyOperation(bankClient);
            moneyOperation.PutMoney(sum);

            DataBase.Entry(bankClient).State = EntityState.Modified;

            OperationLogger operation = new OperationLogger
            {
                OperationType = "Put",
                LogDateTime = DateTime.Now,
                SourceBankID = bankClient.State.BankID,
                SourceCardID = "-",
                DestinationBankID = "-",
                DestinationCardID = "-",
                GotCreditSum = 0,
                PaidCreditSum = 0,
                Money = sum
            };

            DataBase.Operations.Add(operation);
            DataBase.SaveChanges();

            return RedirectToAction("Index", "Clients", new RouteValueDictionary(bankClient));
        }


        public ActionResult TransferMoneyToCard()
        {
            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult TransferMoneyToCard(FormCollection form)
        {
            string bankID = form["BankID"];
            string creditCardID = form["CreditCardID"];
            int sum;

            try
            {
                sum = int.Parse(form["Sum"]);
            }
            catch (FormatException)
            {
                ModelState.AddModelError("Sum", "Entered wrong sum");
                return View();
            }

            List<Client> list = DataBase.Clients.ToList();

            var bankClient = list.FirstOrDefault(p => String.CompareOrdinal(p.State.BankID, bankID) == 0
                && String.CompareOrdinal(p.State.CreditCardID, creditCardID) == 0);

            if (bankClient == null)
            {
                ModelState.AddModelError("CreditCardID", "Some field are wrong");
                return View();
            }

            if (sum <= 0 || sum > 100000000 || sum > bankClient.State.BankMoney)
            {
                ModelState.AddModelError("Sum", "Entered wrong sum");
                return View();
            }

            MoneyOperation moneyOperation = new MoneyOperation(bankClient);
            moneyOperation.TrasferMoneyToCard(sum);

            DataBase.Entry(bankClient).State = EntityState.Modified;

            OperationLogger operation = new OperationLogger
            {
                OperationType = "Transfer",
                LogDateTime = DateTime.Now,
                SourceBankID = bankClient.State.BankID,
                SourceCardID = "-",
                DestinationBankID = "-",
                DestinationCardID = bankClient.State.CreditCardID,
                GotCreditSum = 0,
                PaidCreditSum = 0,
                Money = sum
            };

            DataBase.Operations.Add(operation);
            DataBase.SaveChanges();

            return RedirectToAction("Index", "Clients", new RouteValueDictionary(bankClient));
        }


        public ActionResult TransferMoneyFromCard()
        {
            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult TransferMoneyFromCard(FormCollection form)
        {
            string bankID = form["BankID"];
            string creditCardID = form["CreditCardID"];
            int sum;

            try
            {
                sum = int.Parse(form["Sum"]);
            }
            catch (FormatException)
            {
                ModelState.AddModelError("Sum", "Entered wrong sum");
                return View();
            }

            List<Client> list = DataBase.Clients.ToList();

            var bankClient = list.FirstOrDefault(p => String.CompareOrdinal(p.State.BankID, bankID) == 0
                && String.CompareOrdinal(p.State.CreditCardID, creditCardID) == 0);

            if (bankClient == null)
            {
                ModelState.AddModelError("CreditCardID", "Some field are wrong");
                return View();
            }

            if (sum <= 0 || sum > 100000000 || sum > bankClient.State.CardMoney)
            {
                ModelState.AddModelError("Sum", "Entered wrong sum");
                return View();
            }

            MoneyOperation moneyOperation = new MoneyOperation(bankClient);
            moneyOperation.TrasferMoneyFromCard(sum);

            DataBase.Entry(bankClient).State = EntityState.Modified;

            OperationLogger operation = new OperationLogger
            {
                OperationType = "Transfer",
                LogDateTime = DateTime.Now,
                SourceBankID = "-",
                SourceCardID = bankClient.State.CreditCardID,
                DestinationBankID = bankClient.State.BankID,
                DestinationCardID = "-",
                GotCreditSum = 0,
                PaidCreditSum = 0,
                Money = sum
            };

            DataBase.Operations.Add(operation);
            DataBase.SaveChanges();

            return RedirectToAction("Index", "Clients", new RouteValueDictionary(bankClient));
        }

        public ActionResult TakeCredit()
        {
            if (((Client)Session["client"]).AccountType != "Premium")
            {
                Session.Add("error", new ErrorNotifier {Source = "take"});

                return RedirectToAction("Error", "Clients", (ErrorNotifier)Session["error"]);
            }
            
            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult TakeCredit(FormCollection form)
        {
            string bankID = form["BankID"];
            int sum;

            try
            {
                sum = int.Parse(form["Sum"]);
            }
            catch (FormatException)
            {
                ModelState.AddModelError("Sum", "Entered wrong sum");
                return View();
            }

            List<Client> list = DataBase.Clients.ToList();

            var bankClient = list.FirstOrDefault(p => String.CompareOrdinal(p.State.BankID, bankID) == 0);

            if (bankClient == null)
            {
                ModelState.AddModelError("BankID", "No such ID");
                return View();
            }

            MoneyOperation moneyOperation = new MoneyOperation(bankClient);
            moneyOperation.TakeCredit(sum);

            DataBase.Entry(bankClient).State = EntityState.Modified;

            OperationLogger operation = new OperationLogger
            {
                OperationType = "Take",
                LogDateTime = DateTime.Now,
                SourceBankID = "-",
                SourceCardID = "-",
                DestinationBankID = bankClient.State.BankID,
                DestinationCardID = "-",
                GotCreditSum = sum,
                PaidCreditSum = 0,
                Money = 0
            };

            DataBase.Operations.Add(operation);
            DataBase.SaveChanges();
            
            return RedirectToAction("Index", "Clients", new RouteValueDictionary(bankClient));

        }


        public ActionResult TransferMoneyFromBankIDToBankID()
        {
            if (((Client)Session["client"]).AccountType != "Premium")
            {
                Session.Add("error", new ErrorNotifier { Source = "transfer" });

                return RedirectToAction("Error", "Clients", (ErrorNotifier)Session["error"]);
            }
            return View();
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult TransferMoneyFromBankIDToBankID(FormCollection form)
        {
            string bankID1 = form["BankID1"];
            string BankID2 = form["BankID2"];
            int sum;

            try
            {
                sum = int.Parse(form["Sum"]);
            }
            catch (FormatException)
            {
                ModelState.AddModelError("Sum", "Entered wrong sum");
                return View();
            }

            List<Client> list = DataBase.Clients.ToList();

            Client bankClient1 = null;
            Client bankClient2 = null;
            
            try
            {
                bankClient1 = list.FirstOrDefault(p => String.CompareOrdinal(p.State.BankID, bankID1) == 0);
                bankClient2 = list.FirstOrDefault(p => String.CompareOrdinal(p.State.BankID, BankID2) == 0);
            }
            catch (Exception)
            {
                if (bankClient1 == null)
                {
                    ModelState.AddModelError("BankID1", "Field is wrong");
                    return View();
                }

                if (bankClient2 == null)
                {
                    ModelState.AddModelError("BankID2", "Field is wrong");
                    return View();
                }
            }

            if (sum <= 0 || sum > 100000000 || sum > bankClient1.State.BankMoney)
            {
                ModelState.AddModelError("Sum", "Entered wrong sum");
                return View();
            }

            MoneyOperation moneyOperation = new MoneyOperation(bankClient1);
            moneyOperation.TransferMoneyFromBankIDToBankID(sum, bankClient2);

            DataBase.Entry(bankClient1).State = EntityState.Modified;
            DataBase.Entry(bankClient2).State = EntityState.Modified;

            OperationLogger operation = new OperationLogger
            {
                OperationType = "Transfer",
                LogDateTime = DateTime.Now,
                SourceBankID = bankClient1.State.BankID,
                SourceCardID = "-",
                DestinationBankID = bankClient2.State.BankID,
                DestinationCardID = "-",
                GotCreditSum = 0,
                PaidCreditSum = 0,
                Money = sum
            };

            DataBase.Operations.Add(operation);
            DataBase.SaveChanges();
            
            return RedirectToAction("Index", "Clients", new RouteValueDictionary(bankClient1));
        }


        public ActionResult PayCredit()
        {
            if (((Client)Session["client"]).AccountType != "Premium")
            {
                Session.Add("error", new ErrorNotifier { Source = "pay" });

                return RedirectToAction("Error", "Clients", (ErrorNotifier)Session["error"]);
            }
            return View();
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult PayCredit(FormCollection form)
        {
            string bankID = form["BankID"];

            int sum;

            try
            {
                sum = int.Parse(form["Sum"]);
            }
            catch (FormatException)
            {
                ModelState.AddModelError("Sum", "Entered wrong sum");
                return View();
            }
            
            List<Client> list = DataBase.Clients.ToList();

            var bankClient = list.FirstOrDefault(p => String.CompareOrdinal(p.State.BankID, bankID) == 0);

            if (bankClient == null)
            {
                ModelState.AddModelError("BankID", "No such ID");
                return View();
            }

            if (sum <= 0 || sum > 100000000 || sum > bankClient.State.BankMoney || sum > bankClient.State.CreditMoney)
            {
                ModelState.AddModelError("Sum", "Entered wrong sum");
                return View();
            }

            MoneyOperation moneyOperation = new MoneyOperation(bankClient);
            if (moneyOperation.PayCredit(sum))
            {

                DataBase.Entry(bankClient).State = EntityState.Modified;

                OperationLogger operation = new OperationLogger
                {
                    OperationType = "Pay",
                    LogDateTime = DateTime.Now,
                    SourceBankID = bankClient.State.BankID,
                    SourceCardID = "-",
                    DestinationBankID = "-",
                    DestinationCardID = "-",
                    GotCreditSum = 0,
                    PaidCreditSum = sum,
                    Money = 0
                };

                DataBase.Operations.Add(operation);
                DataBase.SaveChanges();
            }

            else
            {
                ModelState.AddModelError("Sum", "Not enough money on your BankID");
                return View();
            }

            return RedirectToAction("Index", "Clients", new RouteValueDictionary(bankClient));

        }

        public ActionResult Error()
        {

            ErrorNotifier notifier = (ErrorNotifier)Session["error"];

            if (notifier != null)
            {
                switch (notifier.Source)
                {
                    case "take":
                        notifier.Message = "You cannot take credit";
                        notifier.Description = "You must have Premium type of accout to take/pay credits. To change it, link to";
                        break;
                    case "pay":
                        notifier.Message = "You cannot pay credit";
                        notifier.Description = "You must have Premium type of accout to take/pay credits. To change it, link to";
                        break;
                    case "transfer":
                        notifier.Message = "You cannot transfer your money";
                        notifier.Description = "You must have Premium type of accout to transfer your money from BankID to BankID. To change it, link to";
                        break;
                }

                return View(notifier);
            }
            return RedirectToAction("TakeCredit", "Clients");
        }

        
    }
}