//----------------------------------------------------------------------------------------------
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
//----------------------------------------------------------------------------------------------

using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Data.Entity;
using System.Linq;

namespace Commons
{
	/// <summary>
	/// User credentials cache used in Azure Graph API calls
	/// </summary>
	public class AdalTokenCache : TokenCache
	{
		private string _user;
		private PerUserTokenCache _cache;

		// constructor
		public AdalTokenCache(string user)
		{
			// associate the cache to the current user of the web app
			_user = user;

			this.AfterAccess = AfterAccessNotification;
			this.BeforeAccess = BeforeAccessNotification;
			this.BeforeWrite = BeforeWriteNotification;

			using (DataAccess db = new DataAccess()) {
				// look up the entry in the DB
				_cache = db.PerUserTokenCacheList.AsNoTracking().FirstOrDefault(c => c.WebUserUniqueId == _user);

				// place the entry in memory
				this.Deserialize((_cache == null) ? null : _cache.CacheBits);
			}
		}

		// clean up the DB
		public override void Clear()
		{
			base.Clear();

			using (DataAccess db = new DataAccess()) {
				foreach (var cacheEntry in db.PerUserTokenCacheList) {
					db.PerUserTokenCacheList.Remove(cacheEntry);
				}

				db.SaveChanges();
			}
		}

		// Notification raised before ADAL accesses the cache.
		// This is your chance to update the in-memory copy from the DB, if the in-memory version is stale
		void BeforeAccessNotification(TokenCacheNotificationArgs args)
		{
			using (DataAccess db = new DataAccess()) {
				if (_cache == null) {
					// first time access
					_cache = db.PerUserTokenCacheList.AsNoTracking().FirstOrDefault(c => c.WebUserUniqueId == _user);
				} else {   // retrieve last write from the DB
					var status =
						from e in db.PerUserTokenCacheList
						where (e.WebUserUniqueId == _user)
						select new { LastWrite = e.LastWrite };
					// if the in-memory copy is older than the persistent copy
					if (status.AsNoTracking().First().LastWrite > _cache.LastWrite) {
						//// read from from storage, update in-memory copy
						_cache = db.PerUserTokenCacheList.AsNoTracking().FirstOrDefault(c => c.WebUserUniqueId == _user);
					}
				}
			}

			this.Deserialize((_cache == null) ? null : _cache.CacheBits);
		}

		// Notification raised after ADAL accessed the cache.
		// If the HasStateChanged flag is set, ADAL changed the content of the cache
		void AfterAccessNotification(TokenCacheNotificationArgs args)
		{
			using (DataAccess db = new DataAccess()) {
				// if state changed
				if (this.HasStateChanged) {
					// check for an existing entry
					_cache = db.PerUserTokenCacheList.FirstOrDefault(c => c.WebUserUniqueId == _user);

					if (_cache == null) {
						// if no existing entry for that user, create a new one
						_cache = new PerUserTokenCache { WebUserUniqueId = _user, };
					}

					// update the cache contents and the last write timestamp
					_cache.CacheBits = this.Serialize();
					_cache.LastWrite = DateTime.UtcNow;

					// update the DB with modification or new entry
					db.Entry(_cache).State = _cache.Id == 0 ? EntityState.Added : EntityState.Modified;
					db.SaveChanges();
					this.HasStateChanged = false;
				}
			}
		}

		void BeforeWriteNotification(TokenCacheNotificationArgs args)
		{
			// if you want to ensure that no concurrent write take place, use this notification to place a lock on the entry
		}
	}
}