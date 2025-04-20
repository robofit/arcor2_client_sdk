using Arcor2.ClientSdk.Communication;
using Arcor2.ClientSdk.Communication.OpenApi.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Arcor2.ClientSdk.ClientServices.Managers {
    /// <summary>
    ///     Manages lifetime of logic items.
    /// </summary>
    public class LogicItemManager : LockableArcor2ObjectManager<LogicItem> {
        /// <summary>
        ///     Initializes a new instance of <see cref="LogicItemManager" /> class.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="project">The parent project.</param>
        /// <param name="logicItem">The logic item data.</param>
        internal LogicItemManager(Arcor2Session session, ProjectManager project, LogicItem logicItem) : base(
            session, logicItem, logicItem.Id) {
            Project = project;
        }

        /// <summary>
        ///     The parent project.
        /// </summary>
        public ProjectManager Project { get; }

        /// <summary>
        ///     Gets the start action.
        /// </summary>
        /// <value>
        ///     <c>null</c> if START/END node.
        /// </value>
        // Do not cache, can change
        public ActionManager? StartActionManager => Project.ActionPoints
            ?.SelectMany(ap => ap.Actions, (_, action) => action).FirstOrDefault(a => a.Id == Data.Start);

        /// <summary>
        ///     Gets the end action.
        /// </summary>
        /// <value>
        ///     <c>null</c> if START/END node.
        /// </value>
        // Do not cache, can change
        public ActionManager? EndActionManager => Project.ActionPoints
            ?.SelectMany(ap => ap.Actions, (_, action) => action).FirstOrDefault(a => a.Id == Data.End);

        /// <summary>
        ///     Updates the logic item.
        /// </summary>
        /// <remarks>
        ///     The condition tests for equality of a link and a value, where 
        ///     link is in format "{action_id}/{flow}/{result_index}".
        /// </remarks>
        /// <param name="startId">The starting action ID, alternatively, "START" for the first action.</param>
        /// <param name="endId">The ending action ID, alternatively, "END" for the last action.</param>
        /// <param name="condition">The condition, <c>null</c> if not applicable.</param>
        /// <returns></returns>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task UpdateAsync(string startId, string endId, ProjectLogicIf? condition = null) {
            var response =
                await Session.Client.UpdateLogicItemAsync(
                    new UpdateLogicItemRequestArgs(Id, startId, endId, condition!));
            if(!response.Result) {
                throw new Arcor2Exception($"Updating logic item {Id} failed.", response.Messages);
            }
        }

        /// <summary>
        ///     Updates the logic item.
        /// </summary>
        /// <remarks>
        ///     Use overload with string ID parameters to set the "START" and "END".
        /// </remarks>
        /// <param name="start">The starting action.</param>
        /// <param name="end">The ending action.</param>
        /// <param name="condition">The condition, <c>null</c> if not applicable.</param>
        /// <returns></returns>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task UpdateAsync(ActionManager start, ActionManager end, ProjectLogicIf? condition = null) =>
            await UpdateAsync(start.Id, end.Id, condition!);

        /// <summary>
        ///     Removes the logic item.
        /// </summary>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task RemoveAsync() {
            var response = await Session.Client.RemoveLogicItemAsync(new RemoveLogicItemRequestArgs(Id));
            if(!response.Result) {
                throw new Arcor2Exception($"Removing logic item {Id} failed.", response.Messages);
            }
        }

        /// <summary>
        ///     Updates the logic item according to the <paramref name="logicItem" /> instance.
        /// </summary>
        /// <param name="logicItem">Newer version of the logic item.</param>
        /// <exception cref="InvalidOperationException"></exception>
        /// >
        internal void UpdateAccordingToNewObject(LogicItem logicItem) {
            if(Id != logicItem.Id) {
                throw new InvalidOperationException(
                    $"Can't update an LogicItemManager ({Id}) using an logic item data object ({logicItem.Id}) with different ID.");
            }

            UpdateData(logicItem);
        }

        protected override void RegisterHandlers() {
            base.RegisterHandlers();
            Session.Client.LogicItemUpdated += OnLogicItemUpdated;
            Session.Client.LogicItemRemoved += OnLogicItemRemoved;
        }

        protected override void UnregisterHandlers() {
            base.UnregisterHandlers();
            Session.Client.LogicItemUpdated -= OnLogicItemUpdated;
            Session.Client.LogicItemRemoved -= OnLogicItemRemoved;
        }

        private void OnLogicItemRemoved(object sender, LogicItemEventArgs e) {
            if(Project.IsOpen) {
                if(e.Data.Id == Id) {
                    RemoveData();
                    Project.logicItems!.Remove(this);
                    Dispose();
                }
            }
        }

        private void OnLogicItemUpdated(object sender, LogicItemEventArgs e) {
            if(Project.IsOpen) {
                if(e.Data.Id == Id) {
                    UpdateData(e.Data);
                }
            }
        }
    }
}