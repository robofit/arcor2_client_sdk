using System.Collections.Generic;
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
        public ObjectTypeMeta Meta { get; internal set; }
        /// <summary>
        /// The available actions.
        /// </summary>
        public IList<ObjectAction> Actions { get; internal set; } = new List<ObjectAction>();

        public ObjectTypeManager(Arcor2Session session, ObjectTypeMeta meta) : base(session) {
            Meta = meta;
        }


        protected override void RegisterHandlers() {
            Session.client.OnObjectTypeUpdated += OnObjectTypeUpdated;
            Session.client.OnObjectTypeRemoved += OnObjectTypeRemoved;
        }

        private void OnObjectTypeRemoved(object sender, ObjectTypesEventArgs args) {
            foreach (var objectTypeMeta in args.ObjectTypes) {
                if (Meta.Type == objectTypeMeta.Type) {
                    Session.ObjectTypes.Remove(this);
                    Dispose();
                }
            }
        }

        private void OnObjectTypeUpdated(object sender, ObjectTypesEventArgs args) {
            foreach (var objectTypeMeta in args.ObjectTypes) {
                if (Meta.Type == objectTypeMeta.Type) {
                    Meta = objectTypeMeta;
                }
            }
        }

        protected override void UnregisterHandlers() {
            Session.client.OnObjectTypeUpdated -= OnObjectTypeUpdated;
            Session.client.OnObjectTypeRemoved -= OnObjectTypeRemoved;
        }
    }
}
