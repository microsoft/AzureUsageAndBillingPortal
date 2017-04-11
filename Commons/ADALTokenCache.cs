//------------------------------------------ START OF LICENSE -----------------------------------------
//Azure Usage and Billing Insights
//
//Copyright(c) Microsoft Corporation
//
//All rights reserved.
//
//MIT License
//
//Permission is hereby granted, free of charge, to any person obtaining a copy of this software and 
//associated documentation files (the ""Software""), to deal in the Software without restriction, 
//including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,
//and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, 
//subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in all copies or substantial 
//portions of the Software.
//
//THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING 
//BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND 
//NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR 
//OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN 
//CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//----------------------------------------------- END OF LICENSE ------------------------------------------

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