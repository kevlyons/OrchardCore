using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.Extensions.Localization;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.ContentManagement.Display.Models;
using OrchardCore.ContentManagement.Metadata.Models;
using OrchardCore.DisplayManagement.ModelBinding;
using OrchardCore.DisplayManagement.Views;
using OrchardCore.Html.Models;
using OrchardCore.Html.Settings;
using OrchardCore.Html.ViewModels;
using OrchardCore.Infrastructure.Script;
using OrchardCore.Liquid;

namespace OrchardCore.Html.Drivers
{
    public class HtmlBodyPartDisplayDriver : ContentPartDisplayDriver<HtmlBodyPart>
    {
        private readonly ILiquidTemplateManager _liquidTemplateManager;
        private readonly IHtmlScriptSanitizer _htmlScriptSanitizer;
        private readonly HtmlEncoder _htmlEncoder;
        private readonly IStringLocalizer S;

        public HtmlBodyPartDisplayDriver(ILiquidTemplateManager liquidTemplateManager, IStringLocalizer<HtmlBodyPartDisplayDriver> localizer, HtmlEncoder htmlEncoder, IHtmlScriptSanitizer htmlScriptSanitizer)
        {
            _liquidTemplateManager = liquidTemplateManager;
            _htmlScriptSanitizer = htmlScriptSanitizer;
            _htmlEncoder = htmlEncoder;
            S = localizer;
        }

        public override IDisplayResult Display(HtmlBodyPart HtmlBodyPart, BuildPartDisplayContext context)
        {
            return Initialize<HtmlBodyPartViewModel>(GetDisplayShapeType(context), m => BuildViewModelAsync(m, HtmlBodyPart))
                .Location("Detail", "Content:5")
                .Location("Summary", "Content:10");
        }

        public override IDisplayResult Edit(HtmlBodyPart HtmlBodyPart, BuildPartEditorContext context)
        {
            return Initialize<HtmlBodyPartViewModel>(GetEditorShapeType(context), model =>
            {
                model.Html = HtmlBodyPart.Html;
                model.ContentItem = HtmlBodyPart.ContentItem;
                model.HtmlBodyPart = HtmlBodyPart;
                model.TypePartDefinition = context.TypePartDefinition;
            });
        }

        public override async Task<IDisplayResult> UpdateAsync(HtmlBodyPart model, IUpdateModel updater, UpdatePartEditorContext context)
        {
            var viewModel = new HtmlBodyPartViewModel();

            var settings = context.TypePartDefinition.GetSettings<HtmlBodyPartSettings>();

            if (await updater.TryUpdateModelAsync(viewModel, Prefix, t => t.Html))
            {
                if (!string.IsNullOrEmpty(viewModel.Html) && !_liquidTemplateManager.Validate(viewModel.Html, out var errors))
                {
                    var partName = context.TypePartDefinition.DisplayName();
                    context.Updater.ModelState.AddModelError(nameof(model.Html), S["{0} doesn't contain a valid Liquid expression. Details: {1}", partName, string.Join(" ", errors)]);
                }
                else
                {
                    model.Html = (settings.AllowCustomScripts) ? viewModel.Html : _htmlScriptSanitizer.Sanitize(viewModel.Html);
                }
            }

            return Edit(model, context);
        }

        private async ValueTask BuildViewModelAsync(HtmlBodyPartViewModel model, HtmlBodyPart htmlBodyPart)
        {
            model.Html = htmlBodyPart.Html;
            model.HtmlBodyPart = htmlBodyPart;
            model.ContentItem = htmlBodyPart.ContentItem;

            model.Html = await _liquidTemplateManager.RenderAsync(htmlBodyPart.Html, _htmlEncoder, model,
                scope => scope.SetValue("ContentItem", model.ContentItem));
        }
    }
}