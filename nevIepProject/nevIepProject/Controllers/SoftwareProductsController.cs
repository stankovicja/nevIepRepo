using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using nevIepProject.Models;

using PagedList;
using Microsoft.AspNet.Identity;
using System.Threading.Tasks;

using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Auth;
using System.IO;

namespace nevIepProject.Controllers
{
    public class SoftwareProductsController : Controller
    {
        private string _adminId = System.Configuration.ConfigurationManager.AppSettings["AdminId"];
        private static readonly int RESULTS_ON_PAGE = 10;
        private Entities db = new Entities();
        private string _storageConnectionString = System.Configuration.ConfigurationManager.AppSettings["StorageConnectionString"];
		

        // GET: SoftwareProducts
        public async Task<ActionResult> Index(string productName, string priceFrom, string priceTo, int? page)
        {
            IList<SoftwareProduct> list = await db.SoftwareProducts.ToListAsync();

            if (!String.IsNullOrEmpty(productName))
            {
                list = list.Where(x => x.Name.Contains(productName)).ToList();
                page = 1;
            }

            if (!String.IsNullOrEmpty(priceFrom))
            {
                try
                {
                    decimal filter = Decimal.Parse(priceFrom);
                    list = list.Where(x => x.Price >= filter).ToList();
                    page = 1;
                }
                catch (Exception ex) { }
            }


            if (!String.IsNullOrEmpty(priceTo))
            {
                try
                {
                    decimal filter = Decimal.Parse(priceTo);
                    list = list.Where(x => x.Price <= filter).ToList();
                    page = 1;
                }
                catch (Exception ex) { }
            }

            if (User.Identity.GetUserId() != _adminId)
                list = list.Where(u => u.IsDeleted == 0).ToList();


            return View(list.ToPagedList(page ?? 1, RESULTS_ON_PAGE));
        }

        // GET: SoftwareProducts/Details/5
        public async Task<ActionResult> Details(long? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            SoftwareProduct softwareProduct = await db.SoftwareProducts.FindAsync(id);
            if (softwareProduct == null)
            {
                return HttpNotFound();
            }

            string logoUrl = "https://nevenaiep.blob.core.windows.net/" + "iep-container" + "/" + softwareProduct.Logo;
            softwareProduct.Logo = logoUrl;

            string pictureUrl = "https://nevenaiep.blob.core.windows.net/" + "iep-container" + "/" + softwareProduct.Picture;
            softwareProduct.Picture = pictureUrl;

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
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "Id,Name,Version,Description,Logo,Picture,Price,IsDeleted,CreatedBy")] SoftwareProduct softwareProduct,  HttpPostedFileBase logo, HttpPostedFileBase picture)
        {

            if (User.Identity.GetUserId() != _adminId)
                return RedirectToAction("Index");

            if (ModelState.IsValid)
            {

                softwareProduct.AspNetUser = await db.AspNetUsers.FirstAsync(u => u.Id == _adminId);
                softwareProduct.IsDeleted = 0;

                CloudStorageAccount storage = CloudStorageAccount.Parse(_storageConnectionString);
                CloudBlobClient blobClient = storage.CreateCloudBlobClient();
                CloudBlobContainer container = blobClient.GetContainerReference("iep-container");

                string pictureName = Guid.NewGuid().ToString() + "-" + picture.FileName;
                string logoName = Guid.NewGuid().ToString() + "-" + logo.FileName;

                CloudBlockBlob pictureBlob = container.GetBlockBlobReference(pictureName);
                CloudBlockBlob logoBlob = container.GetBlockBlobReference(logoName);

                MemoryStream pictureStream = new MemoryStream();
                picture.InputStream.CopyTo(pictureStream);
                pictureStream.Position = 0;
                await pictureBlob.UploadFromStreamAsync(pictureStream);

                MemoryStream logoStream = new MemoryStream();
                logo.InputStream.CopyTo(logoStream);
                logoStream.Position = 0;
                await logoBlob.UploadFromStreamAsync(logoStream);

                softwareProduct.Picture = pictureName;
                softwareProduct.Logo = logoName;



                db.SoftwareProducts.Add(softwareProduct);
                await db.SaveChangesAsync();
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
