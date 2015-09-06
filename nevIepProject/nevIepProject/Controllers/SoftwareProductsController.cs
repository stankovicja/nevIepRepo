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
using System.Net.Mail;
using log4net;
using iTextSharp.text.pdf;
using System.Xml.Linq;

namespace nevIepProject.Controllers
{
    public class SoftwareProductsController : Controller
    {
        private string _adminId = System.Configuration.ConfigurationManager.AppSettings["AdminId"];
        private static readonly int RESULTS_ON_PAGE = 10;
        private Entities db = new Entities();
        private string _storageConnectionString = System.Configuration.ConfigurationManager.AppSettings["StorageConnectionString"];

        private static readonly ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);


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
                catch (Exception ex) { log.Error("Error Parsing Price"); }
            }


            if (!String.IsNullOrEmpty(priceTo))
            {
                try
                {
                    decimal filter = Decimal.Parse(priceTo);
                    list = list.Where(x => x.Price <= filter).ToList();
                    page = 1;
                }
                catch (Exception ex) { log.Error("Error Parsing Price"); }
            }

            if (User.Identity.GetUserId() != _adminId)
                list = list.Where(u => u.IsDeleted == 0).ToList();

            foreach (SoftwareProduct product in list)
            {
                string logoUrl = "https://nevenaiep.blob.core.windows.net/" + "iep-container" + "/" + product.Logo;
                product.Logo = logoUrl;

            }

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
        [Authorize]
        public ActionResult Create()
        {
            if (User.Identity.GetUserId() != _adminId)
                return RedirectToAction("Index");
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
        [Authorize]
        public async Task<ActionResult> Edit(long? id)
        {
            if (User.Identity.GetUserId() != _adminId)
                return RedirectToAction("Index");

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            SoftwareProduct softwareProduct = await db.SoftwareProducts.FindAsync(id);
            if (softwareProduct == null)
            {
                return HttpNotFound();
            }
            return View(softwareProduct);
        }

        // POST: SoftwareProducts/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "Id,Name,Version,Description,Logo,Picture,Price,IsDeleted,CreatedBy")] SoftwareProduct softwareProduct, HttpPostedFileBase logo, HttpPostedFileBase picture)

        {
            if (User.Identity.GetUserId() != _adminId)
                return RedirectToAction("Index");

            if (ModelState.IsValid)
            {
                CloudStorageAccount storage = CloudStorageAccount.Parse(_storageConnectionString);
                CloudBlobClient blobClient = storage.CreateCloudBlobClient();
                CloudBlobContainer container = blobClient.GetContainerReference("iep-container");

                if (logo!= null)
                {
                    string logoName = Guid.NewGuid().ToString() + "-" + logo.FileName;
                    CloudBlockBlob logoBlob = container.GetBlockBlobReference(logoName);
                    MemoryStream logoStream = new MemoryStream();
                    logo.InputStream.CopyTo(logoStream);
                    logoStream.Position = 0;
                    await logoBlob.UploadFromStreamAsync(logoStream);
                    softwareProduct.Logo = logoName;


                }
                if (picture != null)
                {
                    string pictureName = Guid.NewGuid().ToString() + "-" + picture.FileName;
                    CloudBlockBlob pictureBlob = container.GetBlockBlobReference(pictureName);

                    MemoryStream pictureStream = new MemoryStream();
                    picture.InputStream.CopyTo(pictureStream);
                    pictureStream.Position = 0;
                    await pictureBlob.UploadFromStreamAsync(pictureStream);
                    softwareProduct.Picture = pictureName;

                }
                 
                db.Entry(softwareProduct).State = EntityState.Modified;
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            return View(softwareProduct);
        }

        // GET: SoftwareProducts/Delete/5
        [Authorize]
        public async Task<ActionResult> Delete(long? id)
        {
            if (User.Identity.GetUserId() != _adminId)
                return RedirectToAction("Index");

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            SoftwareProduct softwareProduct = await db.SoftwareProducts.FindAsync(id);
            if (softwareProduct == null)
            {
                return HttpNotFound();
            }
            return View(softwareProduct);
        }

        // POST: SoftwareProducts/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(long id)
        {
            SoftwareProduct softwareProduct = await db.SoftwareProducts.FindAsync(id);
            softwareProduct.IsDeleted = 1;
            await db.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        [Authorize]
        public async Task<ActionResult> MyOrders(string userId, int? page)
        {
            IList<Order> list = await db.Orders.Where(x => x.AspNetUser.Id == userId).ToListAsync();
            return View(list.ToPagedList(page ?? 1, RESULTS_ON_PAGE));
        }

        [Authorize]
        public async Task<ActionResult> DetailsOrder(long id)
        {
            Order order = await db.Orders.Where(x => x.Id == id).FirstOrDefaultAsync();
            return View(order);
        }

        [Authorize]
        public async Task<ActionResult> Buy(long productId)
        {
            string userId = User.Identity.GetUserId();

            SoftwareProduct product = await db.SoftwareProducts.Where(x => x.Id == productId).FirstOrDefaultAsync();

            if (product == null) return RedirectToAction("Index");

            Order order = new Order()
            {
                AspNetUser = db.AspNetUsers.Where(x => x.Id == userId).FirstOrDefault(),
                OrderStatu = db.OrderStatus.Where(x => x.Name == "Waiting").FirstOrDefault(),
                OrderType = db.OrderTypes.Where(x => x.Name == "Buy").FirstOrDefault(),
                SoftwareProduct = product,
                TotalPrice = product.Price,
                createdDate = DateTime.Now
            };


            db.Orders.Add(order);

            await db.SaveChangesAsync();

            CentiliViewModel data = new CentiliViewModel()
            {
                Code = Guid.NewGuid().ToString().Remove(5),
                OrderId = order.Id
            };
            
            return View(data);
        }

        [Authorize]
        public async Task<ActionResult> BuyConfirm(CentiliViewModel data)
        {
            Order order = await db.Orders.Where(x => x.Id == data.OrderId).FirstOrDefaultAsync();

            if (data.Code == data.EnteredCode && order != null)
            {
                order.OrderStatu = await db.OrderStatus.Where(x => x.Name == "Successful").FirstOrDefaultAsync();
                await db.SaveChangesAsync();
                string userId = User.Identity.GetUserId();
                AspNetUser user = await db.AspNetUsers.Where(x => x.Id == userId).FirstOrDefaultAsync();
                if (user != null)
                    SendConfirmationEmail(user.Email, order.Id);
                return RedirectToAction("DetailsOrder", new { id = order.Id });
            }
            else
            {
                if (order != null)
                {
                    order.OrderStatu = await db.OrderStatus.Where(x => x.Name == "Declined").FirstOrDefaultAsync();
                    await db.SaveChangesAsync();
                }
                return View("OrderUnsuccessful");
            }
        }

        private void SendConfirmationEmail(string email, long id)
        {
            MailMessage mail = new MailMessage();

            mail.From = new MailAddress("iepsendmail@gmail.com");
            mail.To.Add(new MailAddress(email));
            mail.Subject = String.Format("Order confirmation - {0}", id);
            mail.Body = "Order successful. Have a nice day!";

            SmtpClient smtp = new SmtpClient("smtp.gmail.com");
            smtp.Credentials = new NetworkCredential("iepsendmail@gmail.com", "MaksPoena");
            smtp.EnableSsl = true;
            smtp.Send(mail);
        }

        [Authorize]
        public async Task<FileResult> ExportOrdersToXml(DateTime from, DateTime to)
        {
            string userId = User.Identity.GetUserId();
            to = to.AddDays(1);
            IList<Order> orders = await db.Orders.Where(x => x.AspNetUser.Id == userId && x.createdDate >= from && x.createdDate <= to).ToListAsync();

            XDocument document = new XDocument(new XElement("Orders"));
            foreach (Order order in orders)
            {
                SoftwareProduct product = order.SoftwareProduct;

                XElement node = new XElement("Order");

                node.Add(new XElement("OrderId", order.Id));
                node.Add(new XElement("ProductPrice", order.TotalPrice));
                node.Add(new XElement("OrderDate", order.createdDate.ToString()));
                node.Add(new XElement("Type", order.OrderType.Name));
                node.Add(new XElement("Status", order.OrderStatu.Name));
                node.Add(new XElement("ProductName", product.Name));
                node.Add(new XElement("ProductDescription", product.Description));

                document.Root.Add(node);
            }

            MemoryStream stream = new MemoryStream();
            document.Save(stream);
            stream.Position = 0;

            string MIMEType = "application/xml";
            return File(stream, MIMEType, String.Format("Orders - {0}.xml", orders.First().AspNetUser.Email));
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
