namespace MailSharp.Core.Dto
{
    public class EmailTemplateDto
    {
        public int Id { get; set; }             // Identificador do template
        public string Name { get; set; }        // Nome do template (ex: "Promoção Janeiro")
        public string Subject { get; set; }     // Assunto do e-mail
        public string Body { get; set; }        // Corpo do e-mail (pode ser HTML)
        public bool IsHtml { get; set; }        // Indica se o corpo é HTML
        public DateTime CreatedAt { get; set; } // Data de criação do template
        public DateTime? UpdatedAt { get; set; } // Data de atualização do template
    }
}
