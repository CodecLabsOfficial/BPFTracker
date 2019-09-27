var BPFTrackConfiguration = (function (bpfTrack) {
    bpfTrack.onLoad = function onLoad(executionContext) {
        bpfTrack.changeLayout(executionContext);
    };

    bpfTrack.changeLayout = function changeLayout(executionContext) {
        var formContext = executionContext.getFormContext();

        if (Xrm.Page.ui.getFormType() == 1) {
            bpfTrack.toggleFields(formContext, false);
            bpfTrack.toggleWebResources(formContext, true);

        }
        else {
            bpfTrack.toggleFields(formContext, true);
            bpfTrack.toggleWebResources(formContext, false);
        }
    };

    bpfTrack.toggleFields = function toggleFields(formContext, setVisible) {
        formContext.getControl("clabs_mainentitylogicalname").setVisible(setVisible);
        //formContext.getControl("clabs_bpfentitylogicalname").setVisible(setVisible);
    };

    bpfTrack.toggleWebResources = function toggleWebResources(formContext, setVisible) {
        formContext.getControl("WebResource_BPF_Entities_Main").setVisible(setVisible);
        //formContext.getControl("WebResource_BPF_Entities_BPF").setVisible(setVisible);
    };

    return bpfTrack;
})({});