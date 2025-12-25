using System.Collections.Generic;
using System.Threading.Tasks;

namespace DynamicAllowListingLib.ServiceTagManagers.NewDayManager
{
  public interface IPersistenceManager<T>
  {
    /// <summary>
    /// Update database state to the provided items. WARNING: Anything not present in the list is lost.
    /// </summary>
    /// <param name="itemsToUpdateWith">List of items to be reflected in db.</param>
    Task UpdateDatabaseStateTo(List<T> itemsToUpdateWith);
    /// <summary>
    /// Get list of all entries in database.
    /// </summary>
    /// <returns>List of items in database.</returns>
    Task<List<T>> GetFromDatabase();
    /// <summary>
    /// Remove given list of items from collection
    /// </summary>
    /// <param name="itemsToBeDeleted">List of the items to be removed from collection</param>
    /// <returns></returns>
    Task RemoveItemsFromDatabase(List<T> itemsToBeDeleted);
    /// <summary>
    /// Get item by document id
    /// </summary>
    /// <param name="id">Id of the item will be selected</param>
    /// <returns></returns>
    Task<T?> GetById(string id);
  }
}