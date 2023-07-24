using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Filespec;
using iText.Kernel.Pdf.Tagging;
using iText.Pdfa;

namespace pdf_to_pdfa_3a.Controllers
{
    public class PDFToPDFAController : ApiController
    {
        [HttpPost]
        public HttpResponseMessage ConvertToPdfa()
        {
            try
            {
                var httpRequest = System.Web.HttpContext.Current.Request;
                var file = httpRequest.Files[0];

                if (file != null && file.ContentLength > 0)
                {
                    using (var inputStream = file.InputStream)
                    using (var outputStream = new MemoryStream())
                    {
                        // Load the input PDF
                        var pdfReader = new PdfReader(inputStream);
                        var pdfDocument = new PdfDocument(pdfReader);

                        // Check if the input PDF has pages
                        if (pdfDocument.GetNumberOfPages() == 0)
                        {
                            return Request.CreateResponse(System.Net.HttpStatusCode.BadRequest, "The input PDF has no pages.");
                        }

                        // Create PDF/A document with conformance level A
                        var pdfWriter = new PdfWriter(outputStream);
                        var pdfADocument = new PdfADocument(pdfWriter, PdfAConformanceLevel.PDF_A_3A, new PdfOutputIntent("Custom", "", "http://www.color.org", "sRGB IEC61966-2.1", new FileStream("sRGB Color Space Profile.icm", FileMode.Open, FileAccess.Read)));

                        try
                        {
                            // Copy the content from the input PDF to the PDF/A document
                            for (int page = 1; page <= pdfDocument.GetNumberOfPages(); page++)
                            {
                                var srcPage = pdfDocument.GetPage(page);
                                var pageCopy = srcPage.CopyTo(pdfADocument);
                                pdfADocument.AddPage(pageCopy);
                            }

                            // Add markinfo dictionary to the catalog
                            var catalog = pdfADocument.GetCatalog().GetPdfObject();
                            var markInfo = new PdfDictionary();
                            markInfo.Put(PdfName.Marked, new PdfBoolean(true));
                            catalog.Put(PdfName.MarkInfo, markInfo);

                            // Embed a file attachment into the PDF
                            //var attachmentFile = new FileStream("attachment.txt", FileMode.Open, FileAccess.Read);
                            //var fileSpec = PdfFileSpec.CreateEmbeddedFileSpec(pdfADocument, attachmentFile, "attachment.txt", "attachment.txt", null, null);

                            //var embeddedFilesNameTree = new PdfDictionary();
                            //embeddedFilesNameTree.Put(new PdfName("attachment.txt"), fileSpec.GetPdfObject());
                            //catalog.Put(PdfName.Names, embeddedFilesNameTree);

                        }
                        finally
                        {
                            // Add StructTreeRoot dictionary
                            var root = pdfADocument.GetCatalog().GetPdfObject().GetAsDictionary(PdfName.StructTreeRoot);
                            if (root == null)
                            {
                                var structTreeRoot = new PdfStructTreeRoot(pdfADocument);
                                pdfADocument.GetCatalog().Put(PdfName.StructTreeRoot, structTreeRoot.GetPdfObject());
                            }

                            // Close the PDF/A document
                            pdfADocument.Close();
                        }

                        // Close the input PDF
                        pdfDocument.Close();

                        // Prepare the response
                        var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                        {
                            Content = new ByteArrayContent(outputStream.ToArray())
                        };

                        // Set the Content-Disposition header with the filename including the current date and time
                        var fileName = DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".pdf";
                        response.Content.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("attachment")
                        {
                            FileName = fileName
                        };
                        response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
                        return response;
                    }
                }
                else
                {
                    return Request.CreateResponse(System.Net.HttpStatusCode.BadRequest, "No file uploaded.");
                }
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(System.Net.HttpStatusCode.InternalServerError, ex);
            }
        }
    }
}
