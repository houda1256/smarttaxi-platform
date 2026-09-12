namespace SmartTaxi.Application.Identity.Documents;

public sealed record DocumentContentResult(Stream Content, string MimeType, string FileName);
