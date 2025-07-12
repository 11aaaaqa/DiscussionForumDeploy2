﻿using System.Net;
using System.Text;
using System.Text.Json;
using System.Web;
using GeneralClassesLib.ApiResponses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Web.MVC.Constants;
using Web.MVC.DTOs.User;
using Web.MVC.Models;
using Web.MVC.Models.ApiResponses;
using Web.MVC.Models.ApiResponses.CommentsResponses;
using Web.MVC.Models.ViewModels.ModeratorViewModels;
using Web.MVC.Models.ViewModels.UserViewModels;
using DiscussionResponse = Web.MVC.Models.ApiResponses.Discussion.DiscussionResponse;
using UserResponse = Web.MVC.Models.ApiResponses.CustUserResponses.UserResponse;

namespace Web.MVC.Controllers
{
    public class UserController : Controller
    {
        private readonly IHttpClientFactory httpClientFactory;
        private readonly string url;

        public UserController(IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            this.httpClientFactory = httpClientFactory;
            url = (string.IsNullOrEmpty(config["Url:Port"]))
                ? $"{config["Url:Protocol"]}://{config["Url:HostName"]}" : $"{config["Url:Protocol"]}://{config["Url:HostName"]}:{config["Url:Port"]}";
        }

        [Route("users/{userName}")]
        [HttpGet]
        public async Task<IActionResult> GetUser(string userName)
        {
            using HttpClient httpClient = httpClientFactory.CreateClient();
            var response = await httpClient.GetAsync($"{url}/api/profile/User/GetUserByUserName?userName={HttpUtility.UrlEncode(userName)}");
            if (!response.IsSuccessStatusCode) return View("ActionError");

            var user = await response.Content.ReadFromJsonAsync<UserResponse>();

            var userRolesResponse = await httpClient.GetAsync($"{url}/api/User/GetUserRolesByUserId/{user.AspNetUserId}");
            if (!userRolesResponse.IsSuccessStatusCode) return View("ActionError");
            var userRoles = await userRolesResponse.Content.ReadFromJsonAsync<List<string>>();
            if (userRoles.Contains(UserRoleConstants.AdminRole))
                ViewBag.UserRole = UserRoleConstants.AdminRole;
            else if (userRoles.Contains(UserRoleConstants.ModeratorRole))
                ViewBag.UserRole = UserRoleConstants.ModeratorRole;
            else
            {
                ViewBag.UserRole = UserRoleConstants.UserRole;
            }

            return View(user);
        }

        [Route("users/{userName}/created/discussions")]
        [HttpGet]
        public async Task<IActionResult> GetUsersCreatedDiscussions(string userName, int pageSize, int pageNumber)
        {
            using HttpClient httpClient = httpClientFactory.CreateClient();
            var response = await httpClient.GetAsync(
                $"{url}/api/Discussion/GetDiscussionsByUserName/{userName}?pageSize={pageSize}&pageNumber={pageNumber}");
            if (!response.IsSuccessStatusCode) return View("ActionError");

            var discussions = await response.Content.ReadFromJsonAsync<List<DiscussionResponse>>();

            var doesNextPageExistResponse = await httpClient.GetAsync(
                $"{url}/api/Discussion/DoesNextDiscussionsByUserNamePageExist/{userName}?pageSize={pageSize}&pageNumber={pageNumber + 1}");
            if (!doesNextPageExistResponse.IsSuccessStatusCode) return View("ActionError");

            bool doesExist = await doesNextPageExistResponse.Content.ReadFromJsonAsync<bool>();
            ViewBag.UserName = userName;

            return View(new GetDiscussionsViewModel
            {
                PageSize = pageSize, CurrentPageNumber = pageNumber, Discussions = discussions, DoesNextPageExist = doesExist,
                PreviousPageNumber = pageNumber - 1, NextPageNumber = pageNumber + 1
            });
        }

        [Route("users/{userName}/created/comments")]
        [HttpGet]
        public async Task<IActionResult> GetUsersCreatedComments(string userName, int pageSize, int pageNumber)
        {
            using HttpClient httpClient = httpClientFactory.CreateClient();
            var response = await httpClient.GetAsync(
                $"{url}/api/Comment/GetCommentsByUserName/{userName}?pageSize={pageSize}&pageNumber={pageNumber}");
            if (!response.IsSuccessStatusCode) return View("ActionError");

            var comments = await response.Content.ReadFromJsonAsync<List<CommentResponse>>();

            var doesNextPageExistResponse = await httpClient.GetAsync(
                $"{url}/api/Comment/DoesNextCommentsByUserNamePageExist/{userName}?pageSize={pageSize}&pageNumber={pageNumber + 1}");
            if (!doesNextPageExistResponse.IsSuccessStatusCode) return View("ActionError");

            bool doesExist = await doesNextPageExistResponse.Content.ReadFromJsonAsync<bool>();
            ViewBag.UserName = userName;

            return View(new GetCommentsViewModel
            {
                PageSize = pageSize, DoesNextPageExist = doesExist, CurrentPageNumber = pageNumber, NextPageNumber = pageNumber + 1,
                PreviousPageNumber = pageNumber - 1, Comments = comments
            });
        }

        [Authorize]
        [Route("users/{userName}/suggested/discussions")]
        [HttpGet]
        public async Task<IActionResult> GetUsersSuggestedDiscussions(string userName, int pageSize, int pageNumber)
        {
            if (userName != User.Identity.Name) return RedirectToAction("AccessIsForbidden","Information");

            using HttpClient httpClient = httpClientFactory.CreateClient();
            var response = await httpClient.GetAsync(
                $"{url}/api/SuggestDiscussion/GetSuggestedDiscussionsByUserName/{userName}?pageSize={pageSize}&pageNumber={pageNumber}");
            if (!response.IsSuccessStatusCode) return View("ActionError");

            var suggestedDiscussions = await response.Content.ReadFromJsonAsync<List<DiscussionResponse>>();

            var doesNextPageExistResponse = await httpClient.GetAsync(
                $"{url}/api/SuggestDiscussion/DoesNextSuggestedDiscussionsByUserNamePageExist/{userName}?pageNumber={pageNumber + 1}&pageSize={pageSize}");
            if (!doesNextPageExistResponse.IsSuccessStatusCode) return View("ActionError");

            bool doesExist = await doesNextPageExistResponse.Content.ReadFromJsonAsync<bool>();
            ViewBag.UserName = userName;

            return View(new GetSuggestedDiscussionsViewModel
            {
                PageSize = pageSize, CurrentPageNumber = pageNumber, DoesNextPageExist = doesExist, NextPageNumber = pageNumber + 1,
                PreviousPageNumber = pageNumber - 1, SuggestedDiscussions = suggestedDiscussions
            });
        }

        [Authorize]
        [Route("users/{userName}/suggested/comments")]
        [HttpGet]
        public async Task<IActionResult> GetUsersSuggestedComments(string userName, int pageSize, int pageNumber)
        {
            if (userName != User.Identity.Name) return RedirectToAction("AccessIsForbidden", "Information");

            using HttpClient httpClient = httpClientFactory.CreateClient();
            var response = await httpClient.GetAsync(
                $"{url}/api/SuggestComment/GetSuggestedCommentsByUserName/{userName}?pageSize={pageSize}&pageNumber={pageNumber}");
            if (!response.IsSuccessStatusCode) return View("ActionError");

            var suggestedComments = await response.Content.ReadFromJsonAsync<List<SuggestedCommentResponse>>();

            var doesNextPageExistResponse = await httpClient.GetAsync(
                $"{url}/api/SuggestComment/DoesNextSuggestedCommentsByUserNamePageExist/{userName}?pageNumber={pageNumber + 1}&pageSize={pageSize}");
            if (!doesNextPageExistResponse.IsSuccessStatusCode) return View("ActionError");

            bool doesExist = await doesNextPageExistResponse.Content.ReadFromJsonAsync<bool>();
            
            ViewBag.UserName = userName;

            return View(new SuggestedCommentsViewModel
            {
                PageSize = pageSize, DoesNextPageExist = doesExist, CurrentPageNumber = pageNumber, NextPageNumber = pageNumber + 1,
                PreviousPageNumber = pageNumber - 1, SuggestedComments = suggestedComments
            });
        }

        [Authorize]
        [Route("users/{userName}/suggested/topics")]
        [HttpGet]
        public async Task<IActionResult> GetUsersSuggestedTopics(string userName, int pageSize, int pageNumber)
        {
            if (userName != User.Identity.Name) return RedirectToAction("AccessIsForbidden", "Information");

            using HttpClient httpClient = httpClientFactory.CreateClient();
            var response = await httpClient.GetAsync(
                $"{url}/api/SuggestTopic/GetSuggestedTopicsByUserName/{userName}?pageSize={pageSize}&pageNumber={pageNumber}");
            if (!response.IsSuccessStatusCode) return View("ActionError");

            var suggestedTopics = await response.Content.ReadFromJsonAsync<List<TopicResponse>>();

            var doesNextPageExistResponse = await httpClient.GetAsync(
                $"{url}/api/SuggestTopic/DoesNextSuggestedTopicsByUserNamePageExist/{userName}?pageNumber={pageNumber + 1}&pageSize={pageSize}");
            if (!doesNextPageExistResponse.IsSuccessStatusCode) return View("ActionError");

            bool doesExist = await doesNextPageExistResponse.Content.ReadFromJsonAsync<bool>();

            ViewBag.UserName = userName;
                 
            return View(new GetSuggestedTopicViewModel
            {
                PageSize = pageSize, DoesNextPageExist = doesExist, CurrentPageNumber = pageNumber, NextPageNumber = pageNumber + 1, 
                PreviousPageNumber = pageNumber - 1, SuggestedTopics = suggestedTopics
            });
        }

        [Authorize]
        [Route("[controller]/ChangeUserName")]
        [HttpGet]
        public IActionResult ChangeUserName(string? returnUrl, Guid userId)
        {
            return View(new ChangeUserNameDto{ReturnUrl = returnUrl, UserId = userId});
        }

        [Authorize]
        [Route("[controller]/ChangeUserName")]
        [HttpPost]
        public async Task<IActionResult> ChangeUserName(ChangeUserNameDto model)
        {
            if (ModelState.IsValid)
            {
                using HttpClient httpClient = httpClientFactory.CreateClient();
                var userResponse = await httpClient.GetAsync($"{url}/api/profile/User/GetUserById/{model.UserId}");
                if (!userResponse.IsSuccessStatusCode) return View("ActionError");

                var user = await userResponse.Content.ReadFromJsonAsync<UserResponse>();
                if (user.UserName != User.Identity.Name) return RedirectToAction("AccessIsForbidden", "Information");

                var existsResponse = await httpClient.GetAsync(
                    $"{url}/api/profile/User/IsNormalizedUserNameAlreadyExists/{model.NewUserName}");
                if (!existsResponse.IsSuccessStatusCode) return View("ActionError");
                bool exists = await existsResponse.Content.ReadFromJsonAsync<bool>();
                if (exists)
                {
                    ModelState.AddModelError(string.Empty, "Имя уже занято");
                    return View(model);
                }

                using StringContent jsonContent =
                    new(JsonSerializer.Serialize(new { model.NewUserName, model.UserId }), Encoding.UTF8, "application/json");

                var response = await httpClient.PatchAsync(
                    $"{url}/api/profile/User/ChangeUserName", jsonContent);
                if (!response.IsSuccessStatusCode) return View("ActionError");

                Response.Cookies.Delete("accessToken");

                var authResponse = await httpClient.GetAsync(
                    $"{url}/api/User/AuthUserByUserName?userName={model.NewUserName}");
                if (!authResponse.IsSuccessStatusCode) return View("ActionError");

                var authenticated = await authResponse.Content.ReadFromJsonAsync<AuthenticatedResponse>();
                Response.Cookies.Append("accessToken", authenticated.Token, new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddMonths(2),
                    HttpOnly = true,
                    SameSite = SameSiteMode.Strict,
                    Secure = true
                });

                if (!string.IsNullOrEmpty(model.ReturnUrl))
                    return LocalRedirect(model.ReturnUrl);

                return LocalRedirect($"/users/{model.NewUserName}");
            }
            return View(model);
        }

        [Authorize]
        [Route("[controller]/ChangePassword")]
        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [Authorize]
        [Route("[controller]/ChangePassword")]
        [HttpPost]
        public async Task<IActionResult> ChangePassword(ChangePasswordDto model, Guid userId, string returnUrl)
        {
            if (ModelState.IsValid)
            {
                model.UserId = userId;
                using HttpClient httpClient = httpClientFactory.CreateClient();
                using StringContent jsonContent =
                    new(JsonSerializer.Serialize(model), Encoding.UTF8, "application/json");
                var response = await httpClient.PatchAsync(
                    $"{url}/api/User/ChangePassword", jsonContent);
                if (response.StatusCode == HttpStatusCode.NotFound) return View("ActionError");
                if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    ModelState.AddModelError(string.Empty, "Старый пароль неверный");
                    return View(model);
                }
                if (!response.IsSuccessStatusCode) return View("ActionError");

                if (!string.IsNullOrEmpty(returnUrl))
                    return LocalRedirect(returnUrl);

                return RedirectToAction("Index", "Home");
            }
            return View(model);
        }
    }
}
