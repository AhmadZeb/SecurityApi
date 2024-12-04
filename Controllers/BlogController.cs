using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecurityApi.DbContext;
using SecurityApi.Helpers;
using SecurityApi.Model;
using System.IO;
using System.Threading.Tasks;

namespace SecurityApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
  
    public class BlogController : ControllerBase
    {
        private readonly SecurityDb _context;
        private readonly IWebHostEnvironment _env;

        public BlogController(SecurityDb context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // Method to upload file and return the file path
        private async Task<string> FileUploadAsync(IFormFile? file)
        {
            if (file == null || file.Length == 0)
                throw new BadHttpRequestException("Invalid file");

            // Check if WebRootPath is null, fallback to ContentRootPath
            string rootPath = _env.WebRootPath ?? _env.ContentRootPath;

            if (string.IsNullOrEmpty(rootPath))
            {
                throw new InvalidOperationException("WebRootPath and ContentRootPath are both null.");
            }

            string uploadsFolder = Path.Combine(rootPath, "uploads");

            // Ensure the uploads directory exists
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            string fileName = Path.GetFileName(file.FileName);
            string filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return $"/uploads/{fileName}"; // Return relative path
        }

        // API Endpoint to upload a blog with an optional image
        [HttpPost("UploadBlog")]
        [Authorize]
        public async Task<IActionResult> UploadBlog([FromForm] Blog model, IFormFile file)
        {
            if (model == null)
                return BadRequest("Blog data is required");

            try
            {
                if (file != null)
                {
                    model.FileName =  UploadFiles.UploadApplicationDocumentFile(file);
                }

                model.CreatedOn = DateTime.UtcNow.AddDays(1);
                model.CreatedBy = "Admin"; // Replace with actual user logic
                _context.Blogs.Add(model);
                await _context.SaveChangesAsync();

                return Ok(new { Message = "Blog uploaded successfully", Data = model });
            }
            catch (Exception ex)
            {
                return BadRequest($"Error: {ex.Message}");
            }
        }

        // API Endpoint to get all blogs
        [HttpGet("GetAllBlogs")]
        public async Task<IActionResult> GetAllBlogs()
        {
            var blogs = await _context.Blogs.ToListAsync();

            var request = HttpContext.Request;
            var rootUrl = $"{request.Scheme}://{request.Host}";
            var uri = new Uri(rootUrl);

            // Update FileName for each blog
            foreach (var blog in blogs)
            {
                blog.FileName = UriHelpers.CombineBaseWithRelative(uri, $"Resources/{blog.FileName}");
            }

            return Ok(blogs);
        }

        [HttpGet("GetBlog/{id}")]
        public async Task<IActionResult> GetBlogById(int id)
        {
            var blog = await _context.Blogs.FindAsync(id);

            if (blog == null)
            {
                return NotFound(new { Message = $"Blog with ID {id} not found." });
            }

            var request = HttpContext.Request;
            var rootUrl = $"{request.Scheme}://{request.Host}";
            var uri = new Uri(rootUrl);

            blog.FileName = UriHelpers.CombineBaseWithRelative(uri, $"Resources/{blog.FileName}");

            return Ok(blog);
        }


        [HttpPut("UpdateBlog/{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateBlog(int id, [FromForm] Blog updatedModel, IFormFile? file)
        {
            // Find the blog by ID
            var blog = await _context.Blogs.FindAsync(id);

            if (blog == null)
            {
                return NotFound(new { Message = "Blog not found" });
            }

            try
            {
                // Update fields if they are provided
                blog.Title = updatedModel.Title ?? blog.Title;
                blog.Summary = updatedModel.Summary ?? blog.Summary;
                blog.Description = updatedModel.Description ?? blog.Description;

                // Handle file upload if a new file is provided
                if (file != null)
                {
                    blog.FileName =  UploadFiles.UploadApplicationDocumentFile(file);
                }

                // Update metadata
                blog.ModifiedOn = DateTime.UtcNow;
                blog.ModifiedBy = "Admin"; // Replace with actual user info, if available

                // Mark the entity as updated
                _context.Blogs.Update(blog);

                // Save changes to the database
                await _context.SaveChangesAsync();

                return Ok(new { Message = "Blog updated successfully", Data = blog });
            }
            catch (Exception ex)
            {
                // Return detailed error message for debugging
                return BadRequest(new { Message = "An error occurred while updating the blog", Error = ex.Message });
            }
        }

        [HttpDelete("DeleteBlog/{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteBlog(int id)
        {
            var blog = await _context.Blogs.FindAsync(id);

            if (blog == null)
            {
                return NotFound(new { Message = "Blog not found" });
            }

            try
            {
                _context.Blogs.Remove(blog);
                await _context.SaveChangesAsync();

                return Ok(new { Message = "Blog deleted successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest($"Error: {ex.Message}");
            }
        }
    }

}

