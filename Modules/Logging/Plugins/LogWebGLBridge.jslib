mergeInto(LibraryManager.library, {
    LogDebug: function(m) {
        var f = typeof UTF8ToString === 'function' ? UTF8ToString : Pointer_stringify;
        console.log(f(m));
    },
  
    LogWarning: function(m) {
        var f = typeof UTF8ToString === 'function' ? UTF8ToString : Pointer_stringify;
        console.warn(f(m));
    },

    LogError: function(m) {
        var f = typeof UTF8ToString === 'function' ? UTF8ToString : Pointer_stringify;
        console.error(f(m));
    },

    LogSyncPersistentDataPath: function() {
        if (typeof FS === 'undefined' || !FS || typeof FS.syncfs !== 'function')
            return;

        var stateOwner = typeof Module !== 'undefined' ? Module : (typeof self !== 'undefined' ? self : {});
        var state = stateOwner.logPersistentDataPathSyncState;
        if (!state)
            state = stateOwner.logPersistentDataPathSyncState = { inProgress: false, pending: false };

        if (state.inProgress) {
            state.pending = true;
            return;
        }

        function runSync() {
            state.inProgress = true;

            FS.syncfs(false, function(error) {
                state.inProgress = false;

                if (error)
                    console.error('[logging] persistent data path sync failed', error);

                if (state.pending) {
                    state.pending = false;
                    runSync();
                }
            });
        }

        runSync();
    }
});
