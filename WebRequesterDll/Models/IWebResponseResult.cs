namespace WebRequesterDll.Models;

public interface IWebResponseResult
{
    /// <summary>
    /// Usually HTML (split off for saving to another file)
    /// </summary>
    string Content { get; set; }

    /// <summary>
    /// Other properties separated so they are separated from the content for saving.
    /// </summary>
    WebReponseInfo Info { get; set; }
}