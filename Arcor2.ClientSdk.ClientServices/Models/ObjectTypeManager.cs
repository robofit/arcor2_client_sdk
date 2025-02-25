using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Arcor2.ClientSdk.ClientServices.Models.Extras;
using Arcor2.ClientSdk.Communication;
using Arcor2.ClientSdk.Communication.OpenApi.Models;

namespace Arcor2.ClientSdk.ClientServices.Models {
    /// <summary>
    /// ARCOR2 Object Type.
    /// </summary>
    public class ObjectTypeManager : Arcor2ObjectManager {
        /// <summary>
        /// Information about the object type.
        /// </summary>
        public ObjectTypeMeta Meta { get; private set; }

        /// <summary>
        /// Information about robot capabilities (if the object type is a robot type).
        /// </summary>
        public RobotMeta? RobotMeta { get; private set; }

        /// <summary>
        /// The available actions.
        /// </summary>
        public IList<ObjectAction> Actions { get; private set; } = new List<ObjectAction>();

        /// <summary>
        /// Initializes a new instance of <see cref="ObjectTypeManager"/> class.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="meta">Object type meta object.</param>
        /// <param name="robotMeta">If the object type is a robot, the robot metadata.</param>
        internal ObjectTypeManager(Arcor2Session session, ObjectTypeMeta meta, RobotMeta? robotMeta = null) : base(session, meta.Type) {
            Meta = meta;
            RobotMeta = robotMeta;
        }

        /// <summary>
        /// Determines whenever the object type is a type or subtype of another object type.
        /// </summary>
        /// <param name="objectType">An object type.</param>
        /// <returns><c>true</c> if this object type is a subtype of the second object type, <c>false</c> otherwise.</returns>
        public bool IsTypeOf(ObjectTypeManager objectType) {
            if (Meta.Type == objectType.Meta.Type) {
                return true;
            }
            if (!string.IsNullOrEmpty(Meta.Base)) {
                var parentType = Session.ObjectTypes.FirstOrDefault(o => o.Meta.Type == Meta.Base);
                if (parentType != null) {
                    return parentType.IsTypeOf(objectType);
                }
                else {
                    Session.logger?.LogWarning($"A object type '{objectType.Meta.Type}' references non-existing parent '{Meta.Base}'.");
                    return false;
                }
            }
            return false;
        }

        /// <summary>
        /// Determines if object is a Robot subtype.
        /// </summary>
        public bool IsRobot() => IsTypeOf(Session.ObjectTypes.First(o => o.Meta.Type == "Robot"));

        /// <summary>
        /// Determines if object is a CollisionObject subtype.
        /// </summary>
        public bool IsCollisionObject() => IsTypeOf(Session.ObjectTypes.First(o => o.Meta.Type == "CollisionObject"));

        /// <summary>
        /// Deletes this object type.
        /// </summary>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task DeleteAsync() {
            var result = await Session.client.RemoveObjectTypeAsync(Id);
            if (result.Result == false) {
                throw new Arcor2Exception($"Failed to delete object type {Id}.", result.Messages);
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
                throw new Arcor2Exception($"Failed to update model of an object type {Id}.", result.Messages);
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
                Actions = actions.Data;
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
        /// <param name="project">Newer version of the project.</param>
        /// <exception cref="InvalidOperationException"></exception>>
        internal void UpdateAccordingToNewObject(ObjectTypeMeta meta, RobotMeta? robotMeta = null) {
            if(Id != meta.Type) {
                throw new InvalidOperationException($"Can't update a ObjectTypeManager ({Id}) using a object type data object ({meta.Type}) with different type.");
            }

            Meta = meta;
            RobotMeta = robotMeta;
        }

        protected override void RegisterHandlers() {
            base.RegisterHandlers();
            Session.client.OnObjectTypeUpdated += OnObjectTypeUpdated;
            Session.client.OnObjectTypeRemoved += OnObjectTypeRemoved;
        }

        protected override void UnregisterHandlers() {
            base.UnregisterHandlers();
            Session.client.OnObjectTypeUpdated -= OnObjectTypeUpdated;
            Session.client.OnObjectTypeRemoved -= OnObjectTypeRemoved;
        }

        private void OnObjectTypeRemoved(object sender, ObjectTypesEventArgs args) {
            foreach (var objectTypeMeta in args.ObjectTypes) {
                if (Id == objectTypeMeta.Type) {
                    Session.ObjectTypes.Remove(this);
                    Dispose();
                }
            }
        }

        private void OnObjectTypeUpdated(object sender, ObjectTypesEventArgs args) {
            foreach (var objectTypeMeta in args.ObjectTypes) {
                if (Id == objectTypeMeta.Type) {
                    Meta = objectTypeMeta;
                }
            }
        }
    }
}
