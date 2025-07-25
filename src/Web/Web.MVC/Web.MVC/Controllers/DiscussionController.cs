﻿using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prometheus;
using Web.MVC.Constants;
using Web.MVC.DTOs.Comment;
using Web.MVC.DTOs.Discussion;
using Web.MVC.Models;
using Web.MVC.Models.ApiResponses.CommentsResponses;
using Web.MVC.Models.ApiResponses.Discussion;
using Web.MVC.Services;

namespace Web.MVC.Controllers
{
    public class DiscussionController : Controller
    {
        private readonly ICheckUserService checkUserService;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly string url;
        private static readonly Counter SuggestedDiscussionsCounter = Metrics.CreateCounter
            ("suggested_discussions", "Number of suggested discussions.");
        private static readonly Counter SuggestedCommentsCounter = Metrics.CreateCounter
            ("suggested_comments", "Number of suggested comments.");
        private static readonly Counter CreatedDiscussionsCounter = Metrics.CreateCounter
            ("created_discussions", "Number of directly created discussions without checking by a moderator.");
        private static readonly Counter CreatedCommentsCounter = Metrics.CreateCounter
            ("created_comments", "Number of directly created comments without checking by a moderator.");

        public DiscussionController(IHttpClientFactory httpClientFactory, ICheckUserService checkUserService, IConfiguration config)
        {
            this.httpClientFactory = httpClientFactory;
            this.checkUserService = checkUserService;
            url = (string.IsNullOrEmpty(config["Url:Port"]))
                ? $"{config["Url:Protocol"]}://{config["Url:HostName"]}" : $"{config["Url:Protocol"]}://{config["Url:HostName"]}:{config["Url:Port"]}";
        }

        [Route("discussions/suggest")]
        [Authorize]
        [HttpGet]
        public IActionResult SuggestDiscussion(string topicName)
        {
            return View(new DiscussionDto{TopicName = topicName});
        }

        [Route("discussions/suggest")]
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> SuggestDiscussion(DiscussionDto model)
        {
            if (ModelState.IsValid)
            {
                using var httpClient = httpClientFactory.CreateClient();

                var getLink =
                    $"{url}/api/profile/User/IsUserBannedByUserName/{User.Identity.Name}?banTypes[]={BanTypeConstants.GeneralBanType}&banTypes[]={BanTypeConstants.DiscussionBanType}";
                var isUserBannedResponse = await httpClient.GetAsync(getLink);
                if (!isUserBannedResponse.IsSuccessStatusCode) return View("ActionError");

                var isUserBanned = await isUserBannedResponse.Content.ReadFromJsonAsync<bool>();
                if (isUserBanned) return View("DiscussionBanned");

                var isUserAchieveNeededCreatedDiscussions =
                    await checkUserService.HasUserCreatedSpecifiedDiscussionsCount(User.Identity.Name, 3);
                if (!isUserAchieveNeededCreatedDiscussions)
                {
                    using StringContent jsonContent = new
                        (JsonSerializer.Serialize(new { model.Title, model.Content, CreatedBy = User.Identity.Name }), Encoding.UTF8, "application/json");

                    var response = await httpClient.PostAsync(
                        $"{url}/api/SuggestDiscussion/SuggestToCreate?topicName={model.TopicName}",
                        jsonContent);
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        SuggestedDiscussionsCounter.Inc();
                        return View("Thanks");
                    }
                       
                }
                else
                {
                    using StringContent jsonContent = new(JsonSerializer.Serialize(new { model.Title, model.TopicName, model.Content,
                            CreatedBy = User.Identity.Name }), Encoding.UTF8, "application/json");
                    var response = await httpClient.PostAsync(
                        $"{url}/api/Discussion/CreateDiscussion",
                        jsonContent);
                    if (!response.IsSuccessStatusCode) return View("ActionError");

                    CreatedDiscussionsCounter.Inc();
                    var discussionId = await response.Content.ReadFromJsonAsync<Guid>();
                    return LocalRedirect($"/discussions/{discussionId}");
                }

                return View("ActionError");
            }

            return View(model);
        }

        [HttpGet]
        [Route("discussions/{id}")]
        public async Task<IActionResult> GetDiscussion(Guid id, int pageSize, int pageNumber)
        {
            using HttpClient httpClient = httpClientFactory.CreateClient();

            var response =
                await httpClient.GetAsync(
                    $"{url}/api/Discussion/GetDiscussionById?id={id}");
            if (!response.IsSuccessStatusCode) return View("ActionError");

            var discussion = await response.Content.ReadFromJsonAsync<DiscussionResponse>();
            ViewBag.Content = discussion.Content; ViewBag.CreatedAt = discussion.CreatedAt; ViewBag.CreatedBy = discussion.CreatedBy;
            ViewBag.Rating = discussion.Rating; ViewBag.Title = discussion.Title; ViewBag.DiscussionId = id;

            ViewBag.IsInBookmarks = false;
            if (User.Identity.IsAuthenticated)
            {
                var isDiscussionInBookmarksResponse = await httpClient.GetAsync(
                    $"{url}/api/Bookmark/IsInBookmarks?userName={User.Identity.Name}&discussionId={id}");
                if (!isDiscussionInBookmarksResponse.IsSuccessStatusCode) return View("ActionError");

                var isInBookmark = await isDiscussionInBookmarksResponse.Content.ReadFromJsonAsync<IsInBookmark>();
                ViewBag.IsInBookmarks = isInBookmark.IsInBookmarks;
                ViewBag.BookmarkId = isInBookmark.BookmarkId;
            }

            var getCommentsResponse = await httpClient.GetAsync(
                $"{url}/api/Comment/GetCommentsByDiscussionId/{id}?pageNumber={pageNumber}&pageSize={pageSize}");
            if (!getCommentsResponse.IsSuccessStatusCode) return View("ActionError");

            var comments = await getCommentsResponse.Content.ReadFromJsonAsync<List<CommentResponse>>();
            ViewBag.Comments = comments;

            var doesNextCommentsPageExistResponse = await httpClient.GetAsync(
                $"{url}/api/Comment/DoesNextCommentsByDiscussionIdPageExist/{id}?pageSize={pageSize}&pageNumber={pageNumber + 1}");
            if (!doesNextCommentsPageExistResponse.IsSuccessStatusCode) return View("ActionError");

            bool doesNextCommentsPageExist = await doesNextCommentsPageExistResponse.Content.ReadFromJsonAsync<bool>();
            ViewBag.DoesNextCommentsPageExist = doesNextCommentsPageExist;
            ViewBag.PageSize = pageSize;
            ViewBag.CurrentPageNumber = pageNumber;
            ViewBag.NextPageNumber = pageNumber + 1;
            ViewBag.PreviousPageNumber = pageNumber - 1;

            return View();
        }

        [Authorize]
        [HttpPost]
        [Route("discussions/{id}")]
        public async Task<IActionResult> SuggestComment(SuggestCommentDto model, Guid id, Guid? repliedOnCommentId, string? repliedOnCommentCreatedBy,
            string? repliedOnCommentContent)
        {
            if (ModelState.IsValid)
            {
                using var httpClient = httpClientFactory.CreateClient();

                var getLink =
                    $"{url}/api/profile/User/IsUserBannedByUserName/{User.Identity.Name}?banTypes[]={BanTypeConstants.GeneralBanType}&banTypes[]={BanTypeConstants.CommentBanType}";
                var isUserBannedResponse = await httpClient.GetAsync(getLink);
                if (!isUserBannedResponse.IsSuccessStatusCode) return View("ActionError");
                
                var isUserBanned = await isUserBannedResponse.Content.ReadFromJsonAsync<bool>();
                if (isUserBanned) return View("CommentBanned", model: id);

                var discussionId = id;
                model.CreatedBy = User.Identity.Name;

                var isAllowedToDirectlyCreateComment = await checkUserService.HasUserCreatedSpecifiedCommentsCount(User.Identity.Name, 7);
                if (!isAllowedToDirectlyCreateComment)
                {
                    using StringContent jsonContent = new(JsonSerializer.Serialize(new
                    {
                        RepliedOnCommentId = repliedOnCommentId,
                        RepliedOnCommentCreatedBy = repliedOnCommentCreatedBy,
                        RepliedOnCommentContent = repliedOnCommentContent,
                        model.CreatedBy,
                        model.Content,
                        DiscussionId = discussionId
                    }), Encoding.UTF8, "application/json");

                    var response = await httpClient.PostAsync($"{url}/api/SuggestComment/Suggest", jsonContent);
                    if (response.IsSuccessStatusCode)
                    {
                        SuggestedCommentsCounter.Inc();
                        return View("ThanksForComment", discussionId);
                    }
                    
                    return View("SomethingWentWrong", discussionId);
                }
                else
                {
                    using StringContent jsonContent = new(JsonSerializer.Serialize(new
                    {
                        RepliedOnCommentId = repliedOnCommentId,
                        RepliedOnCommentCreatedBy = repliedOnCommentCreatedBy,
                        RepliedOnCommentContent = repliedOnCommentContent,
                        discussionId, model.CreatedBy, model.Content }), Encoding.UTF8, "application/json");
                    var response = await httpClient.PostAsync(
                        $"{url}/api/Comment/CreateComment", jsonContent);
                    if (!response.IsSuccessStatusCode)
                        return View("SomethingWentWrong", discussionId);

                    CreatedCommentsCounter.Inc();

                    return LocalRedirect($"/discussions/{id}?pageSize=20&pageNumber=1");
                }
            }
            return View("SomethingWentWrong", id);
        }

        [Authorize(Roles = UserRoleConstants.AdminRole + ", " + UserRoleConstants.ModeratorRole)]
        [HttpPost]
        public async Task<IActionResult> DeleteDiscussion(Guid discussionId, string? returnUrl, string? reportType)
        {
            using HttpClient httpClient = httpClientFactory.CreateClient();
            var response = await httpClient.DeleteAsync(
                $"{url}/api/Discussion/DeleteDiscussionById/{discussionId}");
            if (!response.IsSuccessStatusCode) return View("ActionError");

            if (!string.IsNullOrEmpty(returnUrl))
                return LocalRedirect(returnUrl);

            if (!string.IsNullOrEmpty(reportType))
                return RedirectToAction("Reports", "Report", new {reportType});

            return RedirectToAction("Index", "Home");
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> IncreaseDiscussionRatingByOne(Guid discussionId, string returnUrl)
        {
            using HttpClient httpClient = httpClientFactory.CreateClient();
            using StringContent jsonContent = new(
                JsonSerializer.Serialize(new { discussionId, userNameIncreasedBy = User.Identity.Name }),
                Encoding.UTF8, "application/json");
            var response = await httpClient.PatchAsync(
                $"{url}/api/Discussion/IncreaseDiscussionRatingByOne", jsonContent);
            if (!response.IsSuccessStatusCode) return View("RatingIsAlreadyIncreased", model: returnUrl);

            if(!string.IsNullOrEmpty(returnUrl)) 
                return LocalRedirect(returnUrl);

            return RedirectToAction("Index", "Home");
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> DecreaseDiscussionRatingByOne(Guid discussionId, string returnUrl)
        {
            using HttpClient httpClient = httpClientFactory.CreateClient();
            using StringContent jsonContent = new(
                JsonSerializer.Serialize(new { discussionId, userNameDecreasedBy = User.Identity.Name }),
                Encoding.UTF8, "application/json");
            var response = await httpClient.PatchAsync(
                $"{url}/api/Discussion/DecreaseDiscussionRatingByOne", jsonContent);
            if (!response.IsSuccessStatusCode) return View("RatingIsAlreadyDecreased", model: returnUrl);

            if (!string.IsNullOrEmpty(returnUrl))
                return LocalRedirect(returnUrl);

            return RedirectToAction("Index", "Home");
        }

        [Route("discussions/new")]
        [HttpGet]
        public async Task<IActionResult> GetDiscussionsSortedByNovelty(int pageNumber, int pageSize)
        {
            using HttpClient httpClient = httpClientFactory.CreateClient();
            var response = await httpClient.GetAsync(
                $"{url}/api/Discussion/GetAllDiscussionsSortedByNovelty?pageSize={pageSize}&pageNumber={pageNumber}");
            if (!response.IsSuccessStatusCode) return View("ActionError");

            var discussions = await response.Content.ReadFromJsonAsync<List<DiscussionResponse>>();

            var doesNextPageExistResponse = await httpClient.GetAsync(
                $"{url}/api/Discussion/DoesNextAllDiscussionsPageExist?pageSize={pageSize}&pageNumber={pageNumber + 1}");
            if (!doesNextPageExistResponse.IsSuccessStatusCode) return View("ActionError");

            bool doesExist = await doesNextPageExistResponse.Content.ReadFromJsonAsync<bool>();

            return View(new GetDiscussionsViewModel
            {
                PageSize = pageSize, CurrentPageNumber = pageNumber, DoesNextPageExist = doesExist,
                NextPageNumber = pageNumber + 1, PreviousPageNumber = pageNumber - 1, Discussions = discussions
            });
        }

        [Route("discussions/top/today")]
        [HttpGet]
        public async Task<IActionResult> GetDiscussionsSortedByPopularityForToday(int pageNumber, int pageSize)
        {
            using HttpClient httpClient = httpClientFactory.CreateClient();
            var response = await httpClient.GetAsync(
                $"{url}/api/Discussion/GetAllDiscussionsSortedByPopularityForToday?pageSize={pageSize}&pageNumber={pageNumber}");
            if (!response.IsSuccessStatusCode) return View("ActionError");

            var discussions = await response.Content.ReadFromJsonAsync<List<DiscussionResponse>>();

            var doesNextPageExistResponse = await httpClient.GetAsync(
                $"{url}/api/Discussion/DoesNextDiscussionsForTodayPageExist?pageSize={pageSize}&pageNumber={pageNumber + 1}");
            if (!doesNextPageExistResponse.IsSuccessStatusCode) return View("ActionError");

            bool doesExist = await doesNextPageExistResponse.Content.ReadFromJsonAsync<bool>();

            return View(new GetDiscussionsViewModel
            {
                PageSize = pageSize,
                CurrentPageNumber = pageNumber,
                DoesNextPageExist = doesExist,
                NextPageNumber = pageNumber + 1,
                PreviousPageNumber = pageNumber - 1,
                Discussions = discussions
            });
        }

        [Route("discussions/top/week")]
        [HttpGet]
        public async Task<IActionResult> GetDiscussionsSortedByPopularityForWeek(int pageNumber, int pageSize)
        {
            using HttpClient httpClient = httpClientFactory.CreateClient();
            var response = await httpClient.GetAsync(
                $"{url}/api/Discussion/GetAllDiscussionsSortedByPopularityForWeek?pageSize={pageSize}&pageNumber={pageNumber}");
            if (!response.IsSuccessStatusCode) return View("ActionError");

            var discussions = await response.Content.ReadFromJsonAsync<List<DiscussionResponse>>();

            var doesNextPageExistResponse = await httpClient.GetAsync(
                $"{url}/api/Discussion/DoesNextDiscussionsForWeekPageExist?pageSize={pageSize}&pageNumber={pageNumber + 1}");
            if (!doesNextPageExistResponse.IsSuccessStatusCode) return View("ActionError");

            bool doesExist = await doesNextPageExistResponse.Content.ReadFromJsonAsync<bool>();

            return View(new GetDiscussionsViewModel
            {
                PageSize = pageSize,
                CurrentPageNumber = pageNumber,
                DoesNextPageExist = doesExist,
                NextPageNumber = pageNumber + 1,
                PreviousPageNumber = pageNumber - 1,
                Discussions = discussions
            });
        }

        [Route("discussions/top/month")]
        [HttpGet]
        public async Task<IActionResult> GetDiscussionsSortedByPopularityForMonth(int pageNumber, int pageSize)
        {
            using HttpClient httpClient = httpClientFactory.CreateClient();
            var response = await httpClient.GetAsync(
                $"{url}/api/Discussion/GetAllDiscussionsSortedByPopularityForMonth?pageSize={pageSize}&pageNumber={pageNumber}");
            if (!response.IsSuccessStatusCode) return View("ActionError");

            var discussions = await response.Content.ReadFromJsonAsync<List<DiscussionResponse>>();

            var doesNextPageExistResponse = await httpClient.GetAsync(
                $"{url}/api/Discussion/DoesNextDiscussionsForMonthPageExist?pageSize={pageSize}&pageNumber={pageNumber + 1}");
            if (!doesNextPageExistResponse.IsSuccessStatusCode) return View("ActionError");

            bool doesExist = await doesNextPageExistResponse.Content.ReadFromJsonAsync<bool>();

            return View(new GetDiscussionsViewModel
            {
                PageSize = pageSize,
                CurrentPageNumber = pageNumber,
                DoesNextPageExist = doesExist,
                NextPageNumber = pageNumber + 1,
                PreviousPageNumber = pageNumber - 1,
                Discussions = discussions
            });
        }

        [Route("discussions/top/all-time")]
        [HttpGet]
        public async Task<IActionResult> GetDiscussionsSortedByPopularityForAllTime(int pageNumber, int pageSize)
        {
            using HttpClient httpClient = httpClientFactory.CreateClient();
            var response = await httpClient.GetAsync(
                $"{url}/api/Discussion/GetAllDiscussionsSortedByPopularityForAllTime?pageSize={pageSize}&pageNumber={pageNumber}");
            if (!response.IsSuccessStatusCode) return View("ActionError");

            var discussions = await response.Content.ReadFromJsonAsync<List<DiscussionResponse>>();

            var doesNextPageExistResponse = await httpClient.GetAsync(
                $"{url}/api/Discussion/DoesNextAllDiscussionsPageExist?pageSize={pageSize}&pageNumber={pageNumber + 1}");
            if (!doesNextPageExistResponse.IsSuccessStatusCode) return View("ActionError");

            bool doesExist = await doesNextPageExistResponse.Content.ReadFromJsonAsync<bool>();

            return View(new GetDiscussionsViewModel
            {
                PageSize = pageSize,
                CurrentPageNumber = pageNumber,
                DoesNextPageExist = doesExist,
                NextPageNumber = pageNumber + 1,
                PreviousPageNumber = pageNumber - 1,
                Discussions = discussions
            });
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> DeleteDiscussionById(Guid discussionId, string userNameCreatedBy)
        {
            if (User.Identity.Name != userNameCreatedBy && !User.IsInRole(UserRoleConstants.AdminRole) && !User.IsInRole(UserRoleConstants.ModeratorRole))
            {
                return RedirectToAction("AccessIsForbidden", "Information");
            }
            using HttpClient httpClient = httpClientFactory.CreateClient();
            var response = await httpClient.DeleteAsync($"{url}/api/Discussion/DeleteDiscussionById/{discussionId}");
            if (!response.IsSuccessStatusCode) return View("ActionError");

            return RedirectToAction("Index", "Home");
        }
    }
}
