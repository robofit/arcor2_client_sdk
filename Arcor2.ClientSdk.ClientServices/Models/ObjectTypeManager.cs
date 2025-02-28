using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Arcor2.ClientSdk.ClientServices.Models.Extras;
using Arcor2.ClientSdk.Communication;
using Arcor2.ClientSdk.Communication.OpenApi.Models;

namespace Arcor2.ClientSdk.ClientServices.Models {
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
        /// Determines whenever the object type is a type or subtype of another object type.
        /// </summary>
        /// <param name="objectType">An object type.</param>
        /// <returns><c>true</c> if this object type is a subtype of the second object type, <c>false</c> otherwise.</returns>
        public bool IsTypeOf(ObjectTypeManager objectType) {
            if (Data.Meta.Type == objectType.Data.Meta.Type) {
                return true;
            }
            if (!string.IsNullOrEmpty(Data.Meta.Base)) {
                var parentType = Session.ObjectTypes.FirstOrDefault(o => o.Data.Meta.Type == Data.Meta.Base);
                if (parentType != null) {
                    return parentType.IsTypeOf(objectType);
                }
                else {
                    Session.logger?.LogWarning($"An object type '{objectType.Data.Meta.Type}' references non-existing parent '{Data.Meta.Base}'.");
                    return false;
                }
            }
            return false;
        }

        /// <summary>
        /// Determines if object is a Robot subtype.
        /// </summary>
        public bool IsRobot() => IsTypeOf(Session.ObjectTypes.First(o => o.Data.Meta.Type == "Robot"));

        /// <summary>
        /// Determines if object is a CollisionObject subtype.
        /// </summary>
        public bool IsCollisionObject() => IsTypeOf(Session.ObjectTypes.First(o => o.Data.Meta.Type == "CollisionObject"));

        /// <summary>
        /// Determines if object is a CollisionObject subtype.
        /// </summary>
        public bool IsCamera() => IsTypeOf(Session.ObjectTypes.First(o => o.Data.Meta.Type == "Camera"));

        /// <summary>
        /// Gets scenes which have action object of this object type.
        /// </summary>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task<IList<SceneManager>> GetUsingScenesAsync() {
            var result = await Session.client.GetObjectTypeUsageAsync(new IdArgs(Id));
            if (result.Result == false) {
                throw new Arcor2Exception($"Getting scene usage of object type {Id} failed.", result.Messages);
            }

            return result.Data.Select(id => Session.Scenes.First(s => s.Id == id)).ToList();
        }

        /// <summary>
        /// Deletes this object type.
        /// </summary>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task DeleteAsync() {
            var result = await Session.client.RemoveObjectTypeAsync(Id);
            if(result.Result == false) {
                throw new Arcor2Exception($"Deleting object type {Id} failed.", result.Messages);
            }
        }


        /// <summary>
        /// Updates object model.
        /// </summary>
        /// <param name="model">New object model.</param>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task UpdateObjectModel(CollisionModel model) {
            await LockAsync();
            var result =
                await Session.client.UpdateObjectTypeModelAsync(
                    new UpdateObjectModelRequestArgs(Id, model.ToOpenApiObjectModel(Id)));
            if(result.Result == false) {
                await TryUnlockAsync();
                throw new Arcor2Exception($"Updating model of an object type {Id} failed.", result.Messages);
            }

            await UnlockAsync();
        }

        /// <summary>
        /// Updates the <see cref="Actions"/> collection.
        /// </summary>
        /// <remarks>
        /// This method is called internally on initialization unless you specify otherwise and generally not needed to be ínvoked again.
        /// </remarks>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task ReloadActionsAsync() {
            var actions = await Session.client.GetActionsAsync(new TypeArgs(Id));
            if(actions.Result) {
                Data.Actions = actions.Data;
                OnUpdated();
            }
            else {
                Session.logger?.LogWarning(
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
            Session.client.ObjectTypeUpdated += OnObjectTypeUpdated;
            Session.client.ObjectTypeRemoved += OnObjectTypeRemoved;
        }

        protected override void UnregisterHandlers() {
            base.UnregisterHandlers();
            Session.client.ObjectTypeUpdated -= OnObjectTypeUpdated;
            Session.client.ObjectTypeRemoved -= OnObjectTypeRemoved;
        }

        private void OnObjectTypeRemoved(object sender, ObjectTypesEventArgs args) {
            foreach (var objectTypeMeta in args.ObjectTypes) {
                if (Id == objectTypeMeta.Type) {
                    RemoveData();
                    Session.ObjectTypes.Remove(this);
                    Dispose();
                }
            }
        }

        private void OnObjectTypeUpdated(object sender, ObjectTypesEventArgs args) {
            foreach (var objectTypeMeta in args.ObjectTypes) {
                if (Id == objectTypeMeta.Type) {
                    OnUpdated();
                    Data.Meta = objectTypeMeta;
                }
            }
        }
    }
}
