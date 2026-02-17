namespace Synonms.Structur.Api.Core.Schema.Forms;

public class FormDocument : Document
{
    public FormDocument(Link selfLink, Form form) 
        : base(selfLink)
    {
        Form = form;
    }
    
    public Form Form { get; }
}