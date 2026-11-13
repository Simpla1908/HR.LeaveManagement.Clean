window.leaveUi = {
    openDialog(dialog) {
        dialog.addEventListener("cancel", event => event.preventDefault());
        if (!dialog.open) dialog.showModal();
    }
};