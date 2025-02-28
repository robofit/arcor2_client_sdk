using System;
using System.Collections.Generic;
using System.Linq;
using Arcor2.ClientSdk.ClientServices.Managers;

namespace Arcor2.ClientSdk.ClientServices.Extensions {
    internal static class ListExtensions {

        /// <summary>
        /// This method updates Arcor2ObjectManagers lists according to  parameters.
        /// </summary>
        /// <typeparam name="TManager">The type of <see cref="LockableArcor2ObjectManager{TData}"/>.</typeparam>
        /// <typeparam name="TObject">The object with new data.</typeparam>
        /// <param name="managers">The list of manager objects.</param>
        /// <param name="newObjects">The list of new objects.</param>
        /// <param name="id">Function to get identity of new object.</param>
        /// <param name="update">Function to update the manager using the new object.</param>
        /// <param name="creation">Function to create the manager using the new object.</param>
        /// <returns>Updated list of instances.</returns>
        /// <example>
        /// ActionObjects = ActionObjects.UpdateListOfArcor2Objects(scene.Objects,
        ///                 o => o.Id,
        ///                 (manager, o) => manager.UpdateAccordingToNewObject(o),
        ///                 o => new ActionObjectManager(Session, this, o));
        /// </example>
        public static IList<TManager> UpdateListOfLockableArcor2Objects<TManager, TObject, T>(this IList<TManager>? managers, IList<TObject> newObjects, 
            Func<TObject, string> id, Action<TManager, TObject> update, Func<TObject, TManager> creation)
            where TManager : LockableArcor2ObjectManager<T> where T : class {
            // This is mainly a convenience method for a very reoccurring problem, because
            // we can't just replace the manager instances as a whole due to event registrations.
            // We also can't touch the OpenApi models to introduce some structure, otherwise maintainability nightmare ensues.

            managers ??= new List<TManager>();

            // Update existing or create new
            foreach(var @object in newObjects) {
                var existingObject = managers.FirstOrDefault(a => a.Id == id(@object));
                if(existingObject != null) {
                    update(existingObject, @object);
                }
                else {
                    managers.Add(creation(@object));
                }
            }

            // Dispose missing
            var disposeList = managers
                .Where(a => newObjects.All(s => a.Id != id(s))).ToList();
            foreach (var managerDispose in disposeList) {
                managerDispose.Dispose();
            }

            // Return without disposed.
            return managers.Where(m => !disposeList.Contains(m)).ToList();
        }

        /// <summary>
        /// This method updates Arcor2ObjectManagers lists according to  parameters.
        /// </summary>
        /// <typeparam name="TManager">The type of <see cref="Arcor2ObjectManager{TData}"/>.</typeparam>
        /// <typeparam name="TObject">The object with new data.</typeparam>
        /// <param name="managers">The list of manager objects.</param>
        /// <param name="newObjects">The list of new objects.</param>
        /// <param name="equality">Function to get equality of objects.</param>
        /// <param name="update">Function to update the manager using the new object.</param>
        /// <param name="creation">Function to create the manager using the new object.</param>
        /// <returns>Updated list of instances.</returns>
        public static IList<TManager> UpdateListOfArcor2Objects<TManager, TObject, T>(this IList<TManager>? managers, IList<TObject> newObjects,
            Func<TManager, TObject, bool> equality, Action<TManager, TObject> update, Func<TObject, TManager> creation)
            where TManager : Arcor2ObjectManager<T> where T : class {
            // This is mainly a convenience method for a very reoccurring problem, because
            // we can't just replace the manager instances as a whole due to event registrations.
            // We also can't touch the OpenApi models to introduce some structure, otherwise maintainability nightmare ensues.

            managers ??= new List<TManager>();

            // Update existing or create new
            foreach(var @object in newObjects) {
                var existingObject = managers.FirstOrDefault(a => equality(a, @object));
                if(existingObject != null) {
                    update(existingObject, @object);
                }
                else {
                    managers.Add(creation(@object));
                }
            }

            // Dispose missing
            var disposeList = managers
                .Where(a => newObjects.All(s => !equality(a, s))).ToList();
            foreach(var managerDispose in disposeList) {
                managerDispose.Dispose();
            }

            // Return without disposed.
            return managers.Where(m => !disposeList.Contains(m)).ToList();
        }
    }
}
