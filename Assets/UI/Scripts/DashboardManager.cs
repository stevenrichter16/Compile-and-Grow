using UnityEngine;
using UnityEngine.UIElements;

public sealed class DashboardManager : MonoBehaviour
{
    public enum DashboardState { Sidebar, Detail }

    [Header("UXML Templates")]
    [SerializeField] private VisualTreeAsset sidebarUxml;
    [SerializeField] private VisualTreeAsset cardUxml;
    [SerializeField] private VisualTreeAsset detailViewUxml;
    [SerializeField] private VisualTreeAsset detailSectionUxml;

    [Header("Stylesheets")]
    [SerializeField] private StyleSheet themeStylesheet;
    [SerializeField] private StyleSheet dashboardStylesheet;

    UIDocument _uiDocument;
    DashboardState _state = DashboardState.Sidebar;

    PlantSidebarController _sidebar;
    PlantDetailController _detail;

    GrowthTickManager _tickManager;

    VisualElement _sidebarRoot;
    VisualElement _sidebarWrapper;
    VisualElement _detailRoot;
    VisualElement _detailWrapper;

    void Awake()
    {
        _uiDocument = GetComponent<UIDocument>();
        _sidebar = new PlantSidebarController(cardUxml);
        _detail = new PlantDetailController(detailSectionUxml);
    }

    void Start()
    {
        var root = _uiDocument.rootVisualElement;

        // Apply stylesheets
        if (themeStylesheet != null)
            root.styleSheets.Add(themeStylesheet);
        if (dashboardStylesheet != null)
            root.styleSheets.Add(dashboardStylesheet);

        // Make root transparent to clicks when nothing interactive is showing
        root.pickingMode = PickingMode.Ignore;

        // Clone sidebar — stretch the TemplateContainer so absolute children work
        _sidebarWrapper = sidebarUxml.Instantiate();
        _sidebarWrapper.AddToClassList("stretch-absolute");
        _sidebarRoot = _sidebarWrapper.Q("sidebar");
        root.Add(_sidebarWrapper);
        _sidebar.Initialize(_sidebarRoot);
        _sidebar.OnPlantSelected += OnPlantSelected;

        // Clone detail view — same stretch treatment
        _detailWrapper = detailViewUxml.Instantiate();
        _detailWrapper.AddToClassList("stretch-absolute");
        _detailRoot = _detailWrapper.Q("detail-view");
        root.Add(_detailWrapper);
        _detail.Initialize(_detailRoot);
        _detail.OnBack += OnDetailBack;

        // Sidebar visible immediately, detail hidden
        _sidebar.Show();
        _sidebar.Refresh();
        _detailRoot.style.display = DisplayStyle.None;

        _tickManager = FindFirstObjectByType<GrowthTickManager>();
        SubscribeTick();
    }

    /// <summary>
    /// Returns true if Escape was consumed (detail→sidebar transition).
    /// Returns false if in sidebar state (let caller handle).
    /// </summary>
    public bool HandleEscape()
    {
        if (_state == DashboardState.Detail)
        {
            _detail.Hide();
            _state = DashboardState.Sidebar;
            _sidebar.Show();
            _sidebar.Refresh();
            return true;
        }

        return false;
    }

    /// <summary>
    /// Collapse the sidebar when the terminal opens.
    /// Hides the entire UI Toolkit tree so it cannot intercept pointer events.
    /// </summary>
    public void CollapseSidebar()
    {
        if (_state == DashboardState.Detail)
            _detail.Hide();

        _state = DashboardState.Sidebar;
        _sidebarWrapper.style.display = DisplayStyle.None;
        _detailWrapper.style.display = DisplayStyle.None;
    }

    /// <summary>
    /// Expand the sidebar when the terminal closes.
    /// </summary>
    public void ExpandSidebar()
    {
        if (_state == DashboardState.Detail)
            _detail.Hide();

        _state = DashboardState.Sidebar;
        _sidebarWrapper.style.display = DisplayStyle.Flex;
        _sidebar.Show();
        _sidebar.Refresh();
    }

    void OnPlantSelected(OrganismEntity org)
    {
        _state = DashboardState.Detail;
        _sidebar.Hide();

        var allOrgs = _sidebar.GetOrganisms();
        int index = System.Array.IndexOf(allOrgs, org);
        _detail.ShowOrganism(org, allOrgs, index);
    }

    void OnDetailBack()
    {
        _detail.Hide();
        _state = DashboardState.Sidebar;
        _sidebar.Show();
        _sidebar.Refresh();
    }

    void SubscribeTick()
    {
        if (_tickManager != null)
            _tickManager.OnTickAdvanced += OnTick;
    }

    void UnsubscribeTick()
    {
        if (_tickManager != null)
            _tickManager.OnTickAdvanced -= OnTick;
    }

    void OnTick(long tick)
    {
        switch (_state)
        {
            case DashboardState.Sidebar:
                _sidebar.Refresh();
                break;
            case DashboardState.Detail:
                _detail.Refresh();
                break;
        }
    }

    void OnDestroy()
    {
        UnsubscribeTick();
    }
}
