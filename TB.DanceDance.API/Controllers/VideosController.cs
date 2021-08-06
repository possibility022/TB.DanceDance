﻿using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using TB.DanceDance.Data.Db;
using TB.DanceDance.Data.Models;

namespace TB.DanceDance.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VideosController : ControllerBase
    {
        private readonly ApplicationDbContext context;
        private readonly ILogger<VideosController> _logger;

        public VideosController(ApplicationDbContext context, ILogger<VideosController> logger)
        {
            this.context = context;
            _logger = logger;
        }

        [HttpGet]
        public IEnumerable<VideoInformation> Get()
        {
            if (!Request.Headers.ContainsKey("userHash"))
                throw new Exception();

            var hash = Request.Headers["userHash"].First();
            if (!LoginCache.CheckIfLoggedIn(hash))
            {
                throw new Exception();
            }

            return context.VideosInformation.AsEnumerable();
        }
    }
}
