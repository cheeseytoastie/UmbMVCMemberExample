using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using Umbraco.Web.Mvc;
using Umbraco714.Models;


namespace Umbraco714.Controllers
{
    public class MvcMemberSurfaceController : SurfaceController
    {
        // change me if you're not going to use the content / page name "login" as your page name
        string membersLoginUrl = "/login/";

        // Handles the login
        // you might wonder why this can't be just called MvcMemberRegister
        // http://our.umbraco.org/forum/developers/api-questions/37547-SurfaceController-Form-Post-with-feedback
        [ChildActionOnly]
        [ActionName("MvcMemberLoginRenderForm")]
        public ActionResult MvcMemberLoginRenderForm(MvcMemberModel model)
        {
            string checkUrl = HttpContext.Request.Url.AbsolutePath.ToString();

            // add a trailing / if there isn't one (you can access the same page via http://mydomain.com/login or http://mydomain.com/login/)
            if (checkUrl[checkUrl.Length - 1] != '/')
            {
                checkUrl = checkUrl + "/";
            }

            // if we don't have a session variable and have a request URL then store it
            // we have to store it because if user tries an incorrect login then Current.Request.Url will show /umbraco/RenderMvc 
            // in MVC we won't have "/umbraco/RenderMvc" but I leave this in here just in case
            if (HttpContext.Request.Url != null && HttpContext.Request.Url.AbsolutePath.ToString() != "/umbraco/RenderMvc" && HttpContext.Session["redirectURL"] == null)
            {
                if (checkUrl.ToLower() != membersLoginUrl)
                {
                    HttpContext.Session["redirectURL"] = HttpContext.Request.Url.ToString();
                }
            }

            // set this to be checked by default - wish you could just pass checked=checked
            model.RememberMe = true;
            return PartialView("MvcMemberLogin", model);
        }

        
        [ChildActionOnly]
        [ActionName("MvcMemberRegisterRenderForm")]
        public ActionResult MvcMemberRegisterRenderForm()
        {
            return PartialView("MvcMemberRegister", new MvcMemberRegisterModel());
        }

        [HttpGet]
        [ActionName("MvcMemberValidate")]
        public ActionResult MvcMemberValidateGet(string email, string GUID)
        {
            if (email == null || GUID == null)
            {
                TempData["Status"] = "Error validating your email address";
                return PartialView("MvcMemberValidate", new MvcMemberValidateModel());
            }

            var memberService = Services.MemberService;
            var member = memberService.GetByEmail(email);

            if (member.GetValue("validateguid").ToString().ToLower() == GUID)
            {
                member.IsApproved = true;
                memberService.Save(member);
                TempData["Status"] = "Validated user - you can now login.";
                return PartialView("MvcMemberValidate", new MvcMemberValidateModel());
            }
            else
            {
                TempData["Status"] = "Sorry - we can't seem to validate your email address";
                return PartialView("MvcMemberValidate", new MvcMemberValidateModel());
            }

        }

        // The MemberLogout Action signs out the user and redirects to the site home page:
        [HttpGet]
        public ActionResult MvcMemberLogout()
        {
            // clear the session redirect variable for future logins
            HttpContext.Session["redirectURL"] = null;
            Session.Clear();
            FormsAuthentication.SignOut();
            return Redirect("/");
        }

        // The MemberLoginPost Action checks the entered credentials using the member Umbraco stuff and redirects the user to the same page. Either as logged in, or with a message set in the TempData dictionary:
        [HttpPost]
        [ActionName("MvcMemberLogin")]
        public ActionResult MvcMemberLoginPost(MvcMemberModel model)
        {
            var memberService = Services.MemberService;
            var member = memberService.GetByEmail(model.Email);

            if (member != null && model.Password != null)
            {
                if (!member.IsApproved)
                {
                    TempData["Status"] = "Before you can login you need to validate your email address - check your email for instructions on how to do this.";
                    return RedirectToCurrentUmbracoPage();
                }

                // helper method on Members to login
                if (Members.Login(model.Email, model.Password))
                {
                    // does this work?
                    FormsAuthentication.SetAuthCookie(model.Email, model.RememberMe);
                    return RedirectToCurrentUmbracoPage();
                }
                else
                {
                    TempData["Status"] = "Invalid username or password";
                    return CurrentUmbracoPage();
                }
            }
            else
            {
                TempData["Status"] = "Invalid username or password";
                return CurrentUmbracoPage();
            }
        }


        [HttpPost]
        [ActionName("MvcMemberRegister")]
        public ActionResult MvcMemberRegisterForm(MvcMemberRegisterModel model)
        {
            var memberService = Services.MemberService;

            // Umbraco now uses the email as the members username so we only need to check this
            if (memberService.GetByEmail(model.Email) != null)
            {
                TempData["Status"] = "Email is already in use";
                return CurrentUmbracoPage();
            }

            // create a GUID for the email validation
            string newUserGuid = System.Guid.NewGuid().ToString("N");

            // create the member using the MemberService
            var member = memberService.CreateMember(model.Email, model.Email, model.FirstName + " " + model.LastName, "Member");

            // set custom variables and set to NOT approved - careful with the alias case on your custom
            // properties! e.g. firstName 

            // Set custom properties - we should check our custom properties exist first
            // if (member.HasProperty("firstname"))  - we'll let it bomb instead for learning / setup.
            member.SetValue("firstname", model.FirstName);
            member.SetValue("lastname", model.LastName);
            member.SetValue("validateguid", newUserGuid);
            member.SetValue("mailinglistinclude", model.MailingListInclude);
            member.IsApproved = false;

            // remember to save
            memberService.Save(member);

            // save their password
            memberService.SavePassword(member, model.Password);

            // add to the WebsiteRegistrations group (this is so we can filter access on the members only area)
            memberService.AssignRole(member.Id, "WebsiteRegistrations");

            // Send email logic goes here
            // send email with the GUID 

            // Perhaps you'll redirect them to the members area
            // or just show a success message and ask them to check their email?
            // TempData["Success"] = true;
            // TempData["Status"] = "Registration pending email validation - please check your email to complete the registration process.";
            // return RedirectToCurrentUmbracoPage();
            // OR?
            // return Redirect("/members");

            // for testing I'm returning my guid to test the next bit without an email (e.g. copying and pasting into browser)
            string host = HttpContext.Request.Url.Authority;
            string validateURL = "http://" + host + "/validate?email=" + model.Email + "&guid=" + newUserGuid;

            TempData["Success"] = true;
            TempData["Status"] = "Member created! Test validate url = <a href=\"" + validateURL + "\">" + validateURL + "</a>";

            return RedirectToCurrentUmbracoPage();
        }
    }
}