﻿using CMS.Common;
using CMS.Domain.Infrastructure;
using CMS.Domain.Models;
using CMS.Domain.Storage.Projections;
using CMS.Domain.Storage.Services;
using CMS.Web.Logger;
using CMS.Web.ViewModels;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Mvc;

namespace CMS.Web.Controllers
{
    [Authorize(Roles = Constants.AdminRole)]
    public class UploadPracticepapersController : BaseController
    {
        readonly IApiService _apiService;
        readonly ILogger _logger;
        readonly IUploadPracticepapersService _uploadPracticepapersService;
        readonly IRepository _repository;

        public UploadPracticepapersController(IApiService apiService, ILogger logger, IUploadPracticepapersService uploadPracticepapersService,
            IRepository repository)
        {
            _apiService = apiService;
            _logger = logger;
            _uploadPracticepapersService = uploadPracticepapersService;
            _repository = repository;
        }
        // GET: UploadNotes
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Create()
        {
            var classResult = _apiService.GetClasses();
            var classes = JsonConvert.DeserializeObject<List<ClassProjection>>(classResult);
            var boardResult = _apiService.GetBoards();
            var boards = JsonConvert.DeserializeObject<List<BoardProjection>>(boardResult);


            var classList = (from b in classes
                             select new SelectListItem
                             {
                                 Value = b.ClassId.ToString(),
                                 Text = b.Name
                             }).ToList();

            var boardList = (from b in boards
                             select new SelectListItem
                             {
                                 Value = b.BoardId.ToString(),
                                 Text = b.Name
                             }).ToList();



            return View(new UploadPracticepapersViewModel
            {
                Classes = classList,
                Boards = boardList
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(UploadPracticepapersViewModel viewModel)
        {
            var cmsResult = new CMSResult();
            var classResult = _apiService.GetClasses();
            var classes = JsonConvert.DeserializeObject<List<ClassProjection>>(classResult);
            var boardResult = _apiService.GetBoards();
            var boards = JsonConvert.DeserializeObject<List<BoardProjection>>(boardResult);

            var classList = (from b in classes
                             select new SelectListItem
                             {
                                 Value = b.ClassId.ToString(),
                                 Text = b.Name
                             }).ToList();

            var boardList = (from b in boards
                             select new SelectListItem
                             {
                                 Value = b.BoardId.ToString(),
                                 Text = b.Name
                             }).ToList();

            ViewBag.SubjectId = viewModel.SubjectId;
            if (ModelState.IsValid)
            {
                if (!Constants.PdfType.Contains(viewModel.FilePath.ContentType))
                {
                    cmsResult.Results.Add(new Result { Message = "Please choose pdf file.", IsSuccessful = false });
                    _logger.Warn(cmsResult.Results.FirstOrDefault().Message);
                    Warning(cmsResult.Results.FirstOrDefault().Message, true);
                }
                if (!Constants.ImageTypes.Contains(viewModel.LogoPath.ContentType))
                {
                    cmsResult.Results.Add(new Result { Message = "Please choose Image file.", IsSuccessful = false });
                    _logger.Warn(cmsResult.Results.FirstOrDefault().Message);
                    Warning(cmsResult.Results.FirstOrDefault().Message, true);
                }
                string photoPath = "";
                Guid guid = Guid.NewGuid();
                if (viewModel.LogoPath != null)
                    photoPath = string.Format(@"~/Images/{0}/{1}.jpg", Constants.UploadPracticepapersLogo, guid);
                ViewBag.SubjectId = viewModel.SubjectId;
                if (cmsResult.Success)
                {
                    var result = _uploadPracticepapersService.Save(new UploadPracticepapers
                    {
                        BoardName = viewModel.BoardName,
                        ClassName = viewModel.ClassName,
                        SubjectName = viewModel.SubjectName,
                        Title = viewModel.Title,
                        UploadDate = viewModel.UploadDate,
                        FileName = viewModel.FilePath.FileName,
                        LogoName = photoPath,
                        IsVisible = viewModel.IsVisible,
                        ClassId = viewModel.ClassId,
                        BoardId = viewModel.BoardId,
                        SubjectId = viewModel.SubjectId
                    });

                    if (result.Success)
                    {
                        //string folderPath = Server.MapPath(string.Concat("~/PDF/", Constants.UploadNotesPDF));
                        string folderPath = Server.MapPath(string.Concat("~/PDF/", Constants.UploadPracticepapersPDF));
                        if (!Directory.Exists(folderPath))
                        {
                            Directory.CreateDirectory(folderPath);
                        }
                        var pathToSaveQI = Path.Combine(folderPath, string.Format("{0}", viewModel.FilePath.FileName));
                        if (viewModel.FilePath != null)
                            viewModel.FilePath.SaveAs(pathToSaveQI);

                        string LogoPath = Server.MapPath(string.Concat("~/Images/", Constants.UploadPracticepapersLogo));
                        if (!Directory.Exists(LogoPath))
                        {
                            Directory.CreateDirectory(LogoPath);
                        }
                        var pathToSaveLogo = Path.Combine(LogoPath, string.Format("{0}.jpg", guid));
                        if (viewModel.LogoPath != null)
                            viewModel.LogoPath.SaveAs(pathToSaveLogo);

                        Success(result.Results.FirstOrDefault().Message);
                        ModelState.Clear();
                        viewModel = new UploadPracticepapersViewModel();
                    }
                    else
                    {
                        _logger.Warn(result.Results.FirstOrDefault().Message);
                        Warning(result.Results.FirstOrDefault().Message, true);
                    }
                }
            }

            viewModel.Classes = classList;
            viewModel.Boards = boardList;
            return View(viewModel);
        }

        public ActionResult Edit(int id)
        {
            var projection = _uploadPracticepapersService.GetPracticepapersById(id);

            var classResult = _apiService.GetClasses();
            var classes = JsonConvert.DeserializeObject<List<ClassProjection>>(classResult);
            var boardResult = _apiService.GetBoards();
            var boards = JsonConvert.DeserializeObject<List<BoardProjection>>(boardResult);
            var classId = classes.Where(x => x.Name == projection.ClassName).Select(y => y.ClassId).FirstOrDefault();
            var subjectResult = _apiService.GetSubjects(classId);
            var subjects = JsonConvert.DeserializeObject<List<SubjectProjection>>(subjectResult);
            var boardId = boards.Where(x => x.Name == projection.BoardName).Select(y => y.BoardId).FirstOrDefault();
            var subjectId = subjects.Where(x => x.Name == projection.SubjectName).Select(y => y.SubjectId).FirstOrDefault();

            ViewBag.classList = (from b in classes
                                 select new SelectListItem
                                 {
                                     Value = b.ClassId.ToString(),
                                     Text = b.Name
                                 }).ToList();

            ViewBag.boardList = (from b in boards
                                 select new SelectListItem
                                 {
                                     Value = b.BoardId.ToString(),
                                     Text = b.Name
                                 }).ToList();

            ViewBag.subjectList = (from b in subjects
                                   select new SelectListItem
                                   {
                                       Value = b.SubjectId.ToString(),
                                       Text = b.Name
                                   }).ToList();

            if (projection == null)
            {
                _logger.Warn(string.Format("Practicepapers does not Exists {0}."));
                Warning("Practicepapers does not Exists.");
                return RedirectToAction("Index");
            }

            var viewModel = AutoMapper.Mapper.Map<UploadPracticepapersProjection, UploadPracticepapersEditViewModel>(projection);
            ViewBag.ClassId = classId;
            ViewBag.BoardId = boardId;
            ViewBag.SubjectId = subjectId;

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(UploadPracticepapersEditViewModel viewModel)
        {
            var cmsResult = new CMSResult();
            var classResult = _apiService.GetClasses();
            var classes = JsonConvert.DeserializeObject<List<ClassProjection>>(classResult);
            var boardResult = _apiService.GetBoards();
            var boards = JsonConvert.DeserializeObject<List<BoardProjection>>(boardResult);
            var classId = classes.Where(x => x.ClassId == viewModel.ClassId).Select(y => y.ClassId).FirstOrDefault();
            var subjectResult = _apiService.GetSubjects(classId);
            var subjects = JsonConvert.DeserializeObject<List<SubjectProjection>>(subjectResult);
            var boardId = boards.Where(x => x.BoardId == viewModel.BoardId).Select(y => y.BoardId).FirstOrDefault();
            var subjectId = subjects.Where(x => x.SubjectId == viewModel.SubjectId).Select(y => y.SubjectId).FirstOrDefault();

            ViewBag.classList = (from b in classes
                                 select new SelectListItem
                                 {
                                     Value = b.ClassId.ToString(),
                                     Text = b.Name
                                 }).ToList();

            ViewBag.boardList = (from b in boards
                                 select new SelectListItem
                                 {
                                     Value = b.BoardId.ToString(),
                                     Text = b.Name
                                 }).ToList();

            ViewBag.subjectList = (from b in subjects
                                   select new SelectListItem
                                   {
                                       Value = b.SubjectId.ToString(),
                                       Text = b.Name
                                   }).ToList();

            ViewBag.ClassId = classId;
            ViewBag.BoardId = boardId;
            ViewBag.SubjectId = subjectId;
            if (ModelState.IsValid)
            {
                var uploadPracticepapers = _repository.Project<UploadPracticepapers, bool>(Practicepapers => (from p in Practicepapers where p.UploadPracticepapersId == viewModel.UploadPracticepapersId select p).Any());
                if (!uploadPracticepapers)
                {
                    _logger.Warn(string.Format("Practicepapers not exists."));
                    Danger(string.Format("Practicepapers not exists."));
                }
                if (viewModel.FilePath != null)
                {
                    if (viewModel.FilePath.ContentLength == 0)
                    {
                        cmsResult.Results.Add(new Result { Message = "Pdf is required", IsSuccessful = false });
                    }
                    if (!Constants.PdfType.Contains(viewModel.FilePath.ContentType))
                    {
                        cmsResult.Results.Add(new Result { Message = "Please choose pdf file.", IsSuccessful = false });
                    }
                }

                string logoPath = "";
                if (viewModel.LogoPath != null)
                {
                    logoPath = string.Format(@"~/Images/{0}/{1}", Common.Constants.UploadPracticepapersLogo, viewModel.LogoName.Split('/')[3]);
                    if (viewModel.LogoPath.ContentLength == 0)
                    {
                        cmsResult.Results.Add(new Result { Message = "Logo is required", IsSuccessful = false });
                    }
                    if (!Common.Constants.ImageTypes.Contains(viewModel.LogoPath.ContentType))
                    {
                        _logger.Warn("Please choose either a JPEG, JPG or PNG image.");
                        Warning("Please choose either a JPEG, JPG or PNG image..", true);
                        return View(viewModel);
                    }
                }
                else
                {
                    logoPath = null;
                }

                var fileNameSelect = (viewModel.FilePath != null) ? viewModel.FilePath.FileName : viewModel.FileName;
                var result = _uploadPracticepapersService.Update(new UploadPracticepapers
                {
                    UploadPracticepapersId = viewModel.UploadPracticepapersId,
                    Title = viewModel.Title,
                    ClassName = viewModel.ClassName,
                    BoardName = viewModel.BoardName,
                    SubjectName = viewModel.SubjectName,
                    FileName = fileNameSelect,
                    UploadDate = viewModel.UploadDate,
                    LogoName = (viewModel.LogoPath != null) ? logoPath : viewModel.LogoName,
                    IsVisible = viewModel.IsVisible,
                    ClassId = viewModel.ClassId,
                    BoardId = viewModel.BoardId,
                    SubjectId = viewModel.SubjectId
                });

                if (result.Success)
                {
                    if (viewModel.LogoPath != null)
                    {
                        string PracticepapersLogoPath = Server.MapPath(string.Concat("~/Images/", Common.Constants.UploadPracticepapersLogo));
                        var pathToSave = Path.Combine(PracticepapersLogoPath, viewModel.LogoName.Split('/')[3]);
                        viewModel.LogoPath.SaveAs(pathToSave);
                    }
                    if (viewModel.FilePath != null)
                    {
                        string pdfUploadPath = Server.MapPath(string.Concat("~/PDF/", Constants.UploadResultPDF));
                        var pathToDeletepdf = Path.Combine(pdfUploadPath, string.Format("{0}", viewModel.FileName));
                        if ((System.IO.File.Exists(pathToDeletepdf)))
                        {
                            System.IO.File.Delete(pathToDeletepdf);
                        }

                        if (!Directory.Exists(pdfUploadPath))
                        {
                            Directory.CreateDirectory(pdfUploadPath);
                        }
                        var pathToSaveQI = Path.Combine(pdfUploadPath, string.Format("{0}", viewModel.FilePath.FileName));

                        viewModel.FilePath.SaveAs(pathToSaveQI);
                    }

                    Success(result.Results.FirstOrDefault().Message);
                    ModelState.Clear();
                    return RedirectToAction("Index");
                }
                else
                {
                    _logger.Warn(result.Results.FirstOrDefault().Message);
                    Warning(result.Results.FirstOrDefault().Message, true);
                }
            }
            return View(viewModel);
        }

        public FileResult DownloadPDF(int id)
        {
            var projection = _uploadPracticepapersService.GetPracticepapersById(id);
            string path = "";
            path = Path.Combine(Server.MapPath("~/PDF/UploadPracticepapersPDF"), projection.FileName);
            return File(path, "application/pdf");
        }

        public ActionResult Delete(int id)
        {
            var projection = _uploadPracticepapersService.GetPracticepapersById(id);
            if (projection == null)
            {
                _logger.Warn(string.Format("Practicepapers does not Exists."));
                Warning("Practicepapers does not Exists.");
                return RedirectToAction("Index");
            }
            var viewModel = AutoMapper.Mapper.Map<UploadPracticepapersProjection, UploadPracticepapersEditViewModel>(projection);
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(UploadPracticepapersEditViewModel viewModel)
        {
            string pdfUploadPath = Server.MapPath(string.Concat("~/PDF/", Constants.UploadPracticepapersPDF));
            if (ModelState.IsValid)
            {
                var result = _uploadPracticepapersService.Delete(viewModel.UploadPracticepapersId);
                if (result.Success)
                {
                    var pathToDeletepdf = Path.Combine(pdfUploadPath, string.Format("{0}", viewModel.FileName));
                    if ((System.IO.File.Exists(pathToDeletepdf)))
                    {
                        System.IO.File.Delete(pathToDeletepdf);
                    }
                    Success(result.Results.FirstOrDefault().Message);
                    ModelState.Clear();
                }
                else
                {
                    _logger.Warn(result.Results.FirstOrDefault().Message);
                    Warning(result.Results.FirstOrDefault().Message, true);
                }
            }
            return RedirectToAction("Index");
        }
    }
}