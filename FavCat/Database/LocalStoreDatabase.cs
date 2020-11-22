using System;
using System.Collections.Concurrent;
using System.Threading;
using FavCat.Database.Stored;
using LiteDB;
using MelonLoader;

namespace FavCat.Database
{
    public partial class LocalStoreDatabase : IDisposable
    {
        private readonly LiteDatabase myStoreDatabase;
        private readonly LiteDatabase myFavDatabase;
        private readonly LiteDatabase myImageDatabase;

        internal readonly DatabaseFavoriteHandler<StoredAvatar> AvatarFavorites;
        internal readonly DatabaseFavoriteHandler<StoredWorld> WorldFavorites;
        internal readonly DatabaseFavoriteHandler<StoredPlayer> PlayerFavorites;
        
        internal readonly ILiteCollection<StoredAvatar> myStoredAvatars;
        internal readonly ILiteCollection<StoredPlayer> myStoredPlayers;
        internal readonly ILiteCollection<StoredWorld> myStoredWorlds;
        private readonly ILiteCollection<StoredCategoryOrder> myStoredOrders;

        internal readonly DatabaseImageHandler ImageHandler;

        private readonly ConcurrentQueue<Action> myUpdateThreadQueue = new ConcurrentQueue<Action>();
        private readonly Thread myUpdateThread;
        private volatile bool myIsDisposed = false;

        public LocalStoreDatabase(string databasePath, string imageCachePath)
        {
            myStoreDatabase = new LiteDatabase(new ConnectionString {Filename = $"{databasePath}/favcat-store.db", Connection = ConnectionType.Direct});
            myFavDatabase = new LiteDatabase(new ConnectionString {Filename = $"{databasePath}/favcat-favs.db", Connection = ConnectionType.Direct});
            myImageDatabase = new LiteDatabase(new ConnectionString {Filename = $"{imageCachePath}/favcat-images.db", Connection = ConnectionType.Direct});
            
            myStoreDatabase.Mapper.EmptyStringToNull = false;
            myFavDatabase.Mapper.EmptyStringToNull = false;
            myImageDatabase.Mapper.EmptyStringToNull = false;

            ImageHandler = new DatabaseImageHandler(myImageDatabase);

            myStoredAvatars = myStoreDatabase.GetCollection<StoredAvatar>("avatars");
            myStoredPlayers = myStoreDatabase.GetCollection<StoredPlayer>("players");
            myStoredWorlds = myStoreDatabase.GetCollection<StoredWorld>("worlds");
            
            myStoredOrders = myFavDatabase.GetCollection<StoredCategoryOrder>("category_orders");
            
            AvatarFavorites = new DatabaseFavoriteHandler<StoredAvatar>(myFavDatabase, DatabaseEntity.Avatar, myStoredAvatars, myStoredOrders);
            WorldFavorites = new DatabaseFavoriteHandler<StoredWorld>(myFavDatabase, DatabaseEntity.World, myStoredWorlds, myStoredOrders);
            PlayerFavorites = new DatabaseFavoriteHandler<StoredPlayer>(myFavDatabase, DatabaseEntity.Player, myStoredPlayers, myStoredOrders);

            myUpdateThread = new Thread(UpdateThreadMain);
            myUpdateThread.Start();
        }

        private void UpdateThreadMain()
        {
            while (!myIsDisposed)
            {
                if (myUpdateThreadQueue.TryDequeue(out var action))
                {
                    try
                    {
                        action();
                    }
                    catch (Exception ex)
                    {
                        MelonLogger.LogError($"Exception in DB update thread: {ex}");
                    }
                } else
                    Thread.Sleep(100);
            }
        }

        public void Dispose()
        {
            myIsDisposed = true;
            myUpdateThread.Join();
            myStoreDatabase?.Dispose();
            myFavDatabase.Dispose();
            myImageDatabase.Dispose();
        }
    }
}