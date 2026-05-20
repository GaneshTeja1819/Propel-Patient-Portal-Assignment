using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace UPACIP.Infrastructure.Documents;

public sealed record AppointmentPdfTemplateModel(
    Guid AppointmentId,
    string PatientFullName,
    DateTimeOffset AppointmentStart,
    string ProviderName,
    DateTimeOffset GeneratedAtUtc);

public static class AppointmentPdfTemplate
{
    static AppointmentPdfTemplate()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static byte[] Generate(AppointmentPdfTemplateModel model)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(12));

                page.Header().Column(column =>
                {
                    column.Item().Text("UPACIP - Appointment Confirmation").Bold().FontSize(18);
                    column.Item().PaddingTop(4).Text($"Appointment ID: {model.AppointmentId}");
                });

                page.Content().PaddingVertical(18).Column(column =>
                {
                    column.Spacing(8);
                    column.Item().Text($"Patient: {model.PatientFullName}");
                    column.Item().Text($"Date & Time: {model.AppointmentStart:dddd, MMMM dd yyyy HH:mm zzz}");
                    column.Item().Text($"Provider: {model.ProviderName}");
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Generated: ");
                    text.Span(model.GeneratedAtUtc.ToString("u"));
                });
            });
        });

        return document.GeneratePdf();
    }
}
