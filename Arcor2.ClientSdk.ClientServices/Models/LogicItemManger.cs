using System;
using System.Threading.Tasks;
using Arcor2.ClientSdk.Communication;
using Arcor2.ClientSdk.Communication.OpenApi.Models;

namespace Arcor2.ClientSdk.ClientServices.Models {
    /// <summary>
    /// Manages lifetime of logic items.
    /// </summary>
    public class LogicItemManager : LockableArcor2ObjectManager {
        /// <summary>
        /// The parent project.
        /// </summary>
        internal ProjectManager Project { get; }

        /// <summary>
        /// The data of the logic item.
        /// </summary>
        public LogicItem Data { get; private set; }

        /// <summary>
        /// Initializes a new instance of <see cref="LogicItemManager"/> class.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="project">The parent project.</param>
        /// <param name="logicItem">The logic item data.</param>
        public LogicItemManager(Arcor2Session session, ProjectManager project, LogicItem logicItem) : base(
            session, logicItem.Id) {
            Project = project;
            Data = logicItem;
        }

        /// <summary>
        /// Updates the logic item.
        /// </summary>
        /// <param name="startId">The starting action ID, alternatively, "START" for the first action.</param>
        /// <param name="endId">The ending action ID, alternatively, "END" for the last action.</param>
        /// <param name="condition">The condition, <c>null</c> if not applicable.</param>
        /// <returns></returns>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task UpdateAsync(string startId, string endId, ProjectLogicIf? condition = null) {
            var response = await Session.client.UpdateLogicItemAsync(new UpdateLogicItemRequestArgs(Id, startId, endId, condition!));
            if(!response.Result) {
                throw new Arcor2Exception($"Updating logic item {Id} failed.", response.Messages);
            }
        }

        /// <summary>
        /// Updates the logic item.
        /// </summary>
        /// <remarks>
        /// Use overload with string ID parameters to set the "START" and "END".
        /// </remarks>
        /// <param name="start">The starting action.</param>
        /// <param name="end">The ending action.</param>
        /// <param name="condition">The condition, <c>null</c> if not applicable.</param>
        /// <returns></returns>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task UpdateAsync(ActionManager start, ActionManager end, ProjectLogicIf? condition = null) {
            await UpdateAsync(start.Id, end.Id, condition!);
        }

        /// <summary>
        /// Removes the logic item.
        /// </summary>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task RemoveAsync() {
            var response = await Session.client.RemoveLogicItemAsync(new RemoveLogicItemRequestArgs(Id));
            if(!response.Result) {
                throw new Arcor2Exception($"Removing logic item {Id} failed.", response.Messages);
            }
        }


        /// <summary>
        /// Updates the logic item according to the <paramref name="logicItem"/> instance.
        /// </summary>
        /// <param name="logicItem">Newer version of the logic item.</param>
        /// <exception cref="InvalidOperationException"></exception>>
        internal void UpdateAccordingToNewObject(LogicItem logicItem) {
            if (Id != logicItem.Id) {
                throw new InvalidOperationException(
                    $"Can't update an LogicItemManager ({Id}) using an logic item data object ({logicItem.Id}) with different ID.");
            }

            Data = logicItem;
        }

        protected override void RegisterHandlers() {
            base.RegisterHandlers();
            Session.client.OnLogicItemUpdated += OnLogicItemUpdated;
            Session.client.OnLogicItemRemoved += OnLogicItemRemoved;
        }

        protected override void UnregisterHandlers() {
            base.UnregisterHandlers();
            Session.client.OnLogicItemUpdated -= OnLogicItemUpdated;
            Session.client.OnLogicItemRemoved -= OnLogicItemRemoved;
        }

        private void OnLogicItemRemoved(object sender, LogicItemChangedEventArgs e) {
            if (Project.IsOpen) {
                if (e.Data.Id == Id) {
                    Data = e.Data;
                }
            }
        }

        private void OnLogicItemUpdated(object sender, LogicItemChangedEventArgs e) {
            if(Project.IsOpen) {
                if(e.Data.Id == Id) {
                    Project.LogicItems!.Remove(this);
                    Dispose();
                }
            }
        }
    }
}