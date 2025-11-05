namespace HierarchicalTaskApp.Models
{
    // Görevin durumundan (Todo/Done) bağımsız olarak,
    // "hatalı" olarak işaretlenip işaretlenmediğini tutar
    public enum FlagStatus
    {
        None,
        FlaggedAsIncorrect
    }
}