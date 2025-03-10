using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Arcor2.ClientSdk.ClientServices.Models;
using Arcor2.ClientSdk.Communication;
using Arcor2.ClientSdk.Communication.OpenApi.Models;

namespace Arcor2.ClientSdk.ClientServices.Managers
{
    /// <summary>
    /// Manages lifetime of an object type.
    /// </summary>
    public class ObjectTypeManager : LockableArcor2ObjectManager<ObjectType> {

        /// <summary>
        /// Initializes a new instance of <see cref="ObjectTypeManager"/> class.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="meta">Object type meta object.</param>
        /// <param name="robotMeta">If the object type is a robot, the robot metadata.</param>
        internal ObjectTypeManager(Arcor2Session session, ObjectTypeMeta meta, RobotMeta? robotMeta = null) : base(session, new ObjectType(meta, robotMeta), meta.Type) { }

        /// <summary>
        /// Determines whenever the object type is a type or derived from another object type.
        /// </summary>
        /// <param name="objectType">An object type.</param>
        /// <returns><c>true</c> if this object type is a subtype of the second object type, <c>false</c> otherwise.</returns>
        public bool IsTypeOf(ObjectTypeManager objectType) {
            if (Data.Meta.Type == objectType.Data.Meta.Type) {
                return true;
            }
            if (!string.IsNullOrEmpty(Data.Meta.Base)) {
                if (Parent != null) {
                    return Parent.IsTypeOf(objectType);
                }
                else {
                    if(Id != "Generic") {
                        Session.Logger?.LogWarn($"An object type '{objectType}' references non-existing parent '{Data.Meta.Base}'.");
                    }
                    return false;
                }
            }
            return false;
        }

        /// <summary>
        /// Determines whenever the object type is a type or derived from another object type.
        /// </summary>
        /// <param name="objectType">An object type.</param>
        /// <returns><c>true</c> if this object type is a subtype of the second object type, <c>false</c> otherwise.</returns>
        public bool IsTypeOf(string objectType) {
            if(Data.Meta.Type == objectType &&
               objectType != null! && // This is to prevent Generic being a type of empty string (lets assume empty string will never be a valid type)
               objectType != string.Empty
               ) {
                return true;
            }
            if(!string.IsNullOrEmpty(Data.Meta.Base)) {
                if(Parent != null) {
                    return Parent.IsTypeOf(objectType);
                }
                else {
                    if(Id != "Generic") {
                        Session.Logger?.LogWarn($"An object type '{objectType}' references non-existing parent '{Data.Meta.Base}'.");
                    }
                    return false;
                }
            }
            return false;
        }

        /// <summary>
        /// Determines whenever the object type is derived from another object type.
        /// </summary>
        /// <param name="objectType">An object type.</param>
        /// <returns><c>true</c> if this object type is a subtype of the second object type, <c>false</c> otherwise.</returns>
        public bool IsSubtypeOf(string objectType) {
            if(!string.IsNullOrEmpty(Data.Meta.Base)) {
                if(Parent != null) {
                    return Parent.IsTypeOf(objectType);
                }
                else {
                    if (Id != "Generic") {
                        Session.Logger?.LogWarn($"An object type '{objectType}' references non-existing parent '{Data.Meta.Base}'.");
                    }
                    return false;
                }
            }
            return false;
        }

        /// <summary>
        /// Determines whenever the object type is derived from another object type.
        /// </summary>
        /// <param name="objectType">An object type.</param>
        /// <returns><c>true</c> if this object type is a subtype of the second object type, <c>false</c> otherwise.</returns>
        public bool IsSubtypeOf(ObjectTypeManager objectType) {
            if(!string.IsNullOrEmpty(Data.Meta.Base)) {
                if(Parent != null) {
                    return Parent.IsTypeOf(objectType);
                }
                else {
                    if(Id != "Generic") {
                        Session.Logger?.LogWarn($"An object type '{objectType}' references non-existing parent '{Data.Meta.Base}'.");
                    }
                    return false;
                }
            }
            return false;
        }

        private ObjectTypeManager? parentCached;
        /// <summary>
        /// The parent object type.
        /// </summary>
        /// <value>
        /// <c>null</c> if the object type is the root base type (<c>Generic</c>).
        /// </value>
        public ObjectTypeManager? Parent => parentCached ??= Session.ObjectTypes.FirstOrDefault(o => o.Id == Data.Meta.Base);


        private ObjectTypeManager? sceneParentCached;
        /// <summary>
        /// The required parent action object type.
        /// </summary>
        /// <value>
        /// <c>null</c> if no action object parent is required.
        /// </value>
        public ObjectTypeManager? SceneParent => sceneParentCached ??= Session.ObjectTypes.FirstOrDefault(o => o.Id == Data.Meta.NeedsParentType);

        /// <summary>
        /// Determines if object is derived from the builtin GenericWithPose type.
        /// </summary>
        public bool IsGenericWithPose() => IsTypeOf(Session.ObjectTypes.First(o => o.Data.Meta.Type == "GenericWithPose"));

        /// <summary>
        /// Determines if object is derived from the builtin Robot type.
        /// </summary>
        public bool IsRobot() => IsTypeOf(Session.ObjectTypes.First(o => o.Data.Meta.Type == "Robot"));

        /// <summary>
        /// Determines if object is derived from the builtin MultiArmRobot type.
        /// </summary>
        public bool IsMultiArmRobot() => IsTypeOf(Session.ObjectTypes.First(o => o.Data.Meta.Type == "MultiArmRobot"));

        /// <summary>
        /// Determines if object is derived from the builtin CollisionObject type.
        /// </summary>
        public bool IsCollisionObject() => IsTypeOf(Session.ObjectTypes.First(o => o.Data.Meta.Type == "CollisionObject"));

        /// <summary>
        /// Determines if object is derived from the builtin VirtualCollisionObject type.
        /// </summary>
        public bool IsVirtualCollisionObject() => IsTypeOf(Session.ObjectTypes.First(o => o.Data.Meta.Type == "VirtualCollisionObject"));

        /// <summary>
        /// Determines if object is derived from the builtin CollisionObject type.
        /// </summary>
        public bool IsCamera() => IsTypeOf(Session.ObjectTypes.First(o => o.Data.Meta.Type == "Camera"));

        /// <summary>
        /// Gets scenes which have action object of this object type.
        /// </summary>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task<IList<SceneManager>> GetUsingScenesAsync() {
            var result = await Session.Client.GetObjectTypeUsageAsync(new IdArgs(Id));
            if (result.Result == false) {
                throw new Arcor2Exception($"Getting scene usage of object type {Id} failed.", result.Messages);
            }

            return result.Data.Select(id => Session.Scenes.First(s => s.Id == id)).ToList();
        }

        /// <summary>
        /// Deletes this object type.
        /// </summary>
        /// <remarks>
        /// An action object of this type must not be in an existing scene.
        /// </remarks>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task DeleteAsync() {
            var result = await Session.Client.RemoveObjectTypeAsync(Id);
            if(result.Result == false) {
                throw new Arcor2Exception($"Deleting object type {Id} failed.", result.Messages);
            }
        }


        /// <summary>
        /// Updates object model.
        /// </summary>
        /// <remarks>
        /// Can only be used in opened scene. Only model parameter changes are possible, the model must be of the same type.
        /// </remarks>
        /// <param name="model">New object model.</param>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task UpdateObjectModel(CollisionModel model) {
            await LibraryLockAsync();
            var result =
                await Session.Client.UpdateObjectTypeModelAsync(
                    new UpdateObjectModelRequestArgs(Id, model.ToObjectModel(Id)));
            if(result.Result == false) {
                await TryLibraryUnlockAsync();
                throw new Arcor2Exception($"Updating model of an object type {Id} failed.", result.Messages);
            }

            await LibraryUnlockAsync();
        }

        /// <summary>
        /// Updates the collection of actions.
        /// </summary>
        /// <remarks>
        /// This method is called internally on initialization unless you specify otherwise and generally not needed to be ínvoked again.
        /// </remarks>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task ReloadActionsAsync() {
            var actions = await Session.Client.GetActionsAsync(new TypeArgs(Id));
            if(actions.Result) {
                Data.Actions = actions.Data;
                OnUpdated();
            }
            else {
                Session.Logger?.LogWarn(
                    $"The server returned an error when fetching actions for {Id} object type. Leaving it blank. Error messages: " +
                    string.Join(",", actions.Messages));
            }
        }

        /// <summary>
        /// Updates the object type according to the <paramref name="meta"/> and optional <paramref name="robotMeta"/> instances.
        /// </summary>
        /// <param name="meta">Newer version of the object type.</param>
        /// <param name="robotMeta">Optional newer version of the robot meta.</param>
        /// <exception cref="InvalidOperationException"></exception>>
        internal void UpdateAccordingToNewObject(ObjectTypeMeta meta, RobotMeta? robotMeta = null) {
            if(Id != meta.Type) {
                throw new InvalidOperationException($"Can't update a ObjectTypeManager ({Id}) using a object type data object ({meta.Type}) with different type.");
            }

            Data.Meta = meta;
            Data.RobotMeta = robotMeta ?? Data.RobotMeta;
            OnUpdated();
        }

        protected override void RegisterHandlers() {
            base.RegisterHandlers();
            Session.Client.ObjectTypeUpdated += OnObjectTypeUpdated;
            Session.Client.ObjectTypeRemoved += OnObjectTypeRemoved;
        }

        protected override void UnregisterHandlers() {
            base.UnregisterHandlers();
            Session.Client.ObjectTypeUpdated -= OnObjectTypeUpdated;
            Session.Client.ObjectTypeRemoved -= OnObjectTypeRemoved;
        }

        private void OnObjectTypeRemoved(object sender, ObjectTypesEventArgs args) {
            foreach (var objectTypeMeta in args.Data) {
                if (Id == objectTypeMeta.Type) {
                    RemoveData();
                    Session.objectTypes.Remove(this);
                    Dispose();
                }
            }
        }

        private void OnObjectTypeUpdated(object sender, ObjectTypesEventArgs args) {
            foreach (var objectTypeMeta in args.Data) {
                if (Id == objectTypeMeta.Type) {
                    OnUpdated();
                    Data.Meta = objectTypeMeta;
                }
            }
        }
    }
}
