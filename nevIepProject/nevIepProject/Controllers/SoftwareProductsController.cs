using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using nevIepProject.Models;

namespace nevIepProject.Controllers
{
    public class SoftwareProductsController : Controller
    {
        private Entities db = new Entities();

        // GET: SoftwareProducts
        public ActionResult Index()
        {
            return View(db.SoftwareProducts.ToList());
        }

        // GET: SoftwareProducts/Details/5
        public ActionResult Details(long? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            SoftwareProduct softwareProduct = db.SoftwareProducts.Find(id);
            if (softwareProduct == null)
            {
                return HttpNotFound();
            }
            return View(softwareProduct);
        }

        // GET: SoftwareProducts/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: SoftwareProducts/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id,Name,Version,Description,Logo,Picture,Price,IsDeleted,CreatedBy")] SoftwareProduct softwareProduct)
        {
            if (ModelState.IsValid)
            {
                db.SoftwareProducts.Add(softwareProduct);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(softwareProduct);
        }

        // GET: SoftwareProducts/Edit/5
        public ActionResult Edit(long? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            SoftwareProduct softwareProduct = db.SoftwareProducts.Find(id);
            if (softwareProduct == null)
            {
                return HttpNotFound();
            }
            return View(softwareProduct);
        }

        // POST: SoftwareProducts/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,Name,Version,Description,Logo,Picture,Price,IsDeleted,CreatedBy")] SoftwareProduct softwareProduct)
        {
            if (ModelState.IsValid)
            {
                db.Entry(softwareProduct).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(softwareProduct);
        }

        // GET: SoftwareProducts/Delete/5
        public ActionResult Delete(long? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            SoftwareProduct softwareProduct = db.SoftwareProducts.Find(id);
            if (softwareProduct == null)
            {
                return HttpNotFound();
            }
            return View(softwareProduct);
        }

        // POST: SoftwareProducts/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(long id)
        {
            SoftwareProduct softwareProduct = db.SoftwareProducts.Find(id);
            db.SoftwareProducts.Remove(softwareProduct);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
