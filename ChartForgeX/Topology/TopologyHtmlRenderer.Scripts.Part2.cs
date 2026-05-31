namespace ChartForgeX.Topology;

public sealed partial class TopologyHtmlRenderer
{
    private static string InteractionScriptPart2() => """
          groupIds: unique([detail.sourceGroupId, detail.targetGroupId]),
          edges: [edgeIdentity(detail.element)]
        };
      }
      if (detail.kind === 'group') {
        const nodes = Array.from(wrapper.querySelectorAll('[data-cfx-role="topology-node"]'))
          .filter(node => attr(node, 'data-group-id') === detail.id);
        const edges = Array.from(wrapper.querySelectorAll('[data-cfx-role="topology-edge"]'))
          .filter(edge => attr(edge, 'data-source-group-id') === detail.id || attr(edge, 'data-target-group-id') === detail.id);
        return {
          nodeIds: nodes.map(node => attr(node, 'data-node-id')),
          edgeIds: edges.map(edge => attr(edge, 'data-edge-id')),
          groupIds: unique(edges.flatMap(edge => [attr(edge, 'data-source-group-id'), attr(edge, 'data-target-group-id')]).filter(id => id !== detail.id)),
          edges: edges.map(edgeIdentity)
        };
      }
      return { nodeIds: [], edgeIds: [], groupIds: [], edges: [] };
    };
    const statusColor = status => {
      const key = String(status || '').toLowerCase();
      if (key === 'healthy') return 'var(--cfx-topology-healthy,#16a34a)';
      if (key === 'warning') return 'var(--cfx-topology-warning,#f97316)';
      if (key === 'critical') return 'var(--cfx-topology-critical,#ef4444)';
      if (key === 'disabled') return 'var(--cfx-topology-disabled,#94a3b8)';
      return 'var(--cfx-topology-unknown,#64748b)';
    };
    const panelRowClass = () => {
      if (!selectionPanel) return 'cfx-topology-selection-panel__row';
      const base = (selectionPanel.getAttribute('class') || 'cfx-topology-selection-panel').split(/\s+/)[0] || 'cfx-topology-selection-panel';
      return base + '__row';
    };
    const addSelectionRow = (container, label, value) => {
      if (!container || value === undefined || value === null || value === '') return;
      const row = document.createElement('div');
      row.className = panelRowClass();
      const left = document.createElement('span');
      const right = document.createElement('span');
      left.textContent = label;
      right.textContent = String(value);
      row.append(left, right);
      container.appendChild(row);
    };
    const setSelectionFacts = (container, rows) => {
      if (!container) return;
      container.replaceChildren();
      rows.forEach(row => {
        if (!row || row[1] === undefined || row[1] === null || row[1] === '') return;
        const dt = document.createElement('dt');
        const dd = document.createElement('dd');
        dt.textContent = row[0];
        dd.textContent = String(row[1]);
        container.append(dt, dd);
      });
    };
    const renderSelectionPanel = detail => {
      if (!selectionPanel) return;
      const title = selectionPanel.querySelector('[data-cfx-selection-title]');
      const kind = selectionPanel.querySelector('[data-cfx-selection-kind]');
      const status = selectionPanel.querySelector('[data-cfx-selection-status]');
      const facts = selectionPanel.querySelector('[data-cfx-selection-facts]');
      const meta = selectionPanel.querySelector('[data-cfx-selection-meta]');
      const relatedPanel = selectionPanel.querySelector('[data-cfx-selection-related]');
      if (!detail) {
        selectionPanel.hidden = true;
        selectionPanel.style.removeProperty('--cfx-topology-selection-status-color');
        if (title) title.textContent = 'No item selected';
        if (kind) kind.textContent = 'Selection';
        if (status) status.textContent = '';
        if (facts) facts.replaceChildren();
        if (meta) meta.replaceChildren();
        if (relatedPanel) relatedPanel.replaceChildren();
        return;
      }

      selectionPanel.hidden = false;
      selectionPanel.style.setProperty('--cfx-topology-selection-status-color', statusColor(detail.status));
      const displayTitle = detail.kind === 'node'
        ? (detail.metadata && detail.metadata.name) || (detail.element ? attr(detail.element, 'data-node-label') : '') || detail.id
        : detail.kind === 'edge'
          ? (detail.element ? attr(detail.element, 'data-edge-label') : '') || detail.id
          : (detail.element ? attr(detail.element, 'data-group-label') : '') || detail.id;
      if (title) title.textContent = displayTitle || detail.id || 'Selection';
      if (kind) kind.textContent = detail.kind === 'node' ? (detail.nodeKind || 'Node') : detail.kind === 'edge' ? (detail.edgeKind || 'Edge') : 'Group';
      if (status) status.textContent = detail.status || '';
      setSelectionFacts(facts, [
        ['ID', detail.id],
        ['Status', detail.status],
        ['Group', detail.groupId || detail.sourceGroupId || detail.targetGroupId],
        ['Related nodes', (detail.related && detail.related.nodeIds || []).length],
        ['Related edges', (detail.related && detail.related.edgeIds || []).length]
      ]);
      if (meta) {
        meta.replaceChildren();
        Object.entries(detail.metadata || {}).slice(0, 6).forEach(([key, value]) => addSelectionRow(meta, key, value));
        Object.entries(detail.metrics || {}).slice(0, 4).forEach(([key, value]) => addSelectionRow(meta, key, value));
      }
      if (relatedPanel) {
        relatedPanel.replaceChildren();
        (detail.related && detail.related.edges || []).slice(0, 5).forEach(edge => {
          const label = edge.kind || 'Edge';
          const value = [edge.sourceNodeId, edge.targetNodeId].filter(Boolean).join(' -> ');
          addSelectionRow(relatedPanel, label, value || edge.id);
        });
      }
    };
    const clear = () => {
      wrapper.removeAttribute('data-cfx-selection-kind');
      wrapper.removeAttribute('data-cfx-selection-id');
      wrapper.removeAttribute('data-cfx-selection-status');
      wrapper.querySelectorAll('.cfx-topology-html-selected,.cfx-topology-html-muted,.cfx-topology-html-related').forEach(item => {
        item.classList.remove('cfx-topology-html-selected', 'cfx-topology-html-muted', 'cfx-topology-html-related');
        item.removeAttribute('aria-selected');
      });
      clearForceFocusLabels();
      renderSelectionPanel(null);
    };
    const clearHover = () => {
      wrapper.removeAttribute('data-cfx-hover-kind');
      wrapper.removeAttribute('data-cfx-hover-id');
      wrapper.querySelectorAll('.cfx-topology-html-hovered,.cfx-topology-html-hover-muted,.cfx-topology-html-hover-related').forEach(item => {
        item.classList.remove('cfx-topology-html-hovered', 'cfx-topology-html-hover-muted', 'cfx-topology-html-hover-related');
      });
      restoreForceFocusLabels();
    };
    const identity = element => {
      const role = element.getAttribute('data-cfx-role') || '';
      const base = {
        role,
        elementId: attr(element, 'id'),
        status: attr(element, 'data-cfx-status'),
        selected: attr(element, 'data-cfx-selected') === 'true',
        metadata: collectPrefixed(element, 'data-cfx-meta-'),
        metrics: collectPrefixed(element, 'data-cfx-metric-'),
        element
      };
      if (role === 'topology-node') {
        return {
          ...base,
          kind: 'node',
          id: attr(element, 'data-node-id'),
          nodeKind: attr(element, 'data-node-kind'),
          groupId: attr(element, 'data-group-id'),
          displayMode: attr(element, 'data-node-display-mode'),
          badge: attr(element, 'data-node-badge'),
          color: attr(element, 'data-node-color'),
          iconId: attr(element, 'data-node-icon-id'),
          iconPack: attr(element, 'data-node-icon-pack'),
          iconLabel: attr(element, 'data-node-icon-label'),
          iconShape: attr(element, 'data-node-icon-shape'),
          iconArtwork: attr(element, 'data-node-icon-artwork'),
          artworkSource: attr(element, 'data-node-artwork-source'),
          longitude: attr(element, 'data-node-longitude'),
          latitude: attr(element, 'data-node-latitude'),
          geoVisible: attr(element, 'data-node-geo-visible')
        };
      }
      if (role === 'topology-edge') {
        return {
          ...base,
          kind: 'edge',
          id: attr(element, 'data-edge-id'),
          edgeKind: attr(element, 'data-edge-kind'),
          sourceNodeId: attr(element, 'data-source-node-id'),
          targetNodeId: attr(element, 'data-target-node-id'),
          sourceGroupId: attr(element, 'data-source-group-id'),
          targetGroupId: attr(element, 'data-target-group-id'),
          sourcePort: attr(element, 'data-source-port'),
          targetPort: attr(element, 'data-target-port'),
          routeLane: attr(element, 'data-route-lane'),
          layoutInference: attr(element, 'data-edge-layout-inference'),
          muted: attr(element, 'data-edge-muted') === 'true',
          lineStyle: attr(element, 'data-edge-line-style'),
          route: {
            strategy: attr(element, 'data-route-strategy'),
            curve: attr(element, 'data-route-curve'),
            controlX: attr(element, 'data-route-control-x'),
            controlY: attr(element, 'data-route-control-y'),
            corridor: attr(element, 'data-route-corridor'),
            candidateCount: attr(element, 'data-route-candidate-count'),
            fallbackReason: attr(element, 'data-route-fallback-reason'),
            segmentCount: attr(element, 'data-route-segment-count'),
            obstacleCount: attr(element, 'data-route-obstacle-count'),
            obstacleHits: attr(element, 'data-route-obstacle-hits'),
            labelObstacleHits: attr(element, 'data-route-label-obstacle-hits'),
            overlapScore: attr(element, 'data-route-overlap-score'),
            offset: attr(element, 'data-route-offset'),
            waypointCount: attr(element, 'data-waypoint-count')
          }
        };
      }
      if (role === 'topology-group') {
        return {
          ...base,
          kind: 'group',
          id: attr(element, 'data-group-id'),
          symbol: attr(element, 'data-group-symbol'),
          color: attr(element, 'data-group-color'),
          iconId: attr(element, 'data-group-icon-id'),
          iconPack: attr(element, 'data-group-icon-pack'),
          iconLabel: attr(element, 'data-group-icon-label'),
          iconShape: attr(element, 'data-group-icon-shape'),
          iconArtwork: attr(element, 'data-group-icon-artwork'),
          longitude: attr(element, 'data-group-longitude'),
          latitude: attr(element, 'data-group-latitude'),
          geoVisible: attr(element, 'data-group-geo-visible'),
          layoutPolicy: attr(element, 'data-group-layout-policy'),
          appliedLayoutPolicy: attr(element, 'data-group-applied-layout-policy'),
          callout: {
            visualRole: attr(element, 'data-cfx-visual-role'),
            placement: attr(element, 'data-callout-placement'),
            anchorX: attr(element, 'data-callout-anchor-x'),
            anchorY: attr(element, 'data-callout-anchor-y'),
            nodeCount: attr(element, 'data-callout-node-count'),
            healthyCount: attr(element, 'data-callout-healthy-count'),
            warningCount: attr(element, 'data-callout-warning-count'),
            criticalCount: attr(element, 'data-callout-critical-count'),
            unknownCount: attr(element, 'data-callout-unknown-count'),
            disabledCount: attr(element, 'data-callout-disabled-count')
          }
        };
      }
      return { ...base, kind: 'unknown', id: '' };
    };
    const findSelection = request => {
      if (!request || !request.kind || !request.id) return null;
      return Array.from(wrapper.querySelectorAll(selectables)).find(element => {
        const detail = identity(element);
        return detail.kind === request.kind && detail.id === request.id;
      }) || null;
    };
    const elementKey = element => {
      const detail = identity(element);
      return detail.kind + ':' + detail.id;
    };
    const publicDetail = element => {
      const detail = identity(element);
      detail.related = related(detail);
      delete detail.element;
      return detail;
    };
    const relatedElements = detail => {
      const items = [];
      const seen = new Set();
      const add = (kind, id) => {
        if (!id) return;
        const key = kind + ':' + id;
        if (seen.has(key) || (detail.kind === kind && detail.id === id)) return;
        const element = findSelection({ kind, id });
        if (!element) return;
        seen.add(key);
        items.push(element);
      };
      (detail.related.nodeIds || []).forEach(id => add('node', id));
      (detail.related.edgeIds || []).forEach(id => add('edge', id));
      (detail.related.groupIds || []).forEach(id => add('group', id));
      return items;
    };
    const focusRelated = (element, offset) => {
      const detail = identity(element);
      detail.related = related(detail);
      const candidates = relatedElements(detail);
      if (!candidates.length) return false;
      const sourceKey = detail.kind + ':' + detail.id;
      const previousTarget = wrapper.getAttribute('data-cfx-navigation-source') === sourceKey ? wrapper.getAttribute('data-cfx-navigation-target') || '' : '';
      const previousIndex = candidates.findIndex(candidate => elementKey(candidate) === previousTarget);
      const nextIndex = previousIndex >= 0 ? (previousIndex + offset + candidates.length) % candidates.length : (offset > 0 ? 0 : candidates.length - 1);
      const target = candidates[nextIndex];
      const targetKey = elementKey(target);
      wrapper.setAttribute('data-cfx-navigation-source', sourceKey);
      wrapper.setAttribute('data-cfx-navigation-target', targetKey);
      if (target.focus) target.focus({ preventScroll: true });
      wrapper.dispatchEvent(new CustomEvent('cfx-topology-navigate', { bubbles: true, detail: { chartId: attr(wrapper, 'data-chart-id'), from: publicDetail(element), to: publicDetail(target) } }));
      return true;
    };
    const applyRelatedClasses = (detail, classPrefix, selfClass) => {
      const relatedEdges = new Set(detail.related.edgeIds || []);
      const relatedNodes = new Set(detail.related.nodeIds || []);
      const relatedGroups = new Set(detail.related.groupIds || []);
      const relatedClass = classPrefix + 'related';
      const mutedClass = classPrefix + 'muted';
      wrapper.querySelectorAll('[data-cfx-role="topology-edge"]').forEach(edge => {
        const isSelf = detail.kind === 'edge' && attr(edge, 'data-edge-id') === detail.id;
        const isRelated = isSelf || relatedEdges.has(attr(edge, 'data-edge-id'));
        edge.classList.toggle(relatedClass, isRelated);
        edge.classList.toggle(mutedClass, !isRelated);
      });
      wrapper.querySelectorAll('[data-cfx-role="topology-edge-label"]').forEach(label => {
        const isSelf = detail.kind === 'edge' && attr(label, 'data-edge-id') === detail.id;
        const isRelated = isSelf || relatedEdges.has(attr(label, 'data-edge-id'));
        label.classList.toggle(relatedClass, isRelated);
        label.classList.toggle(mutedClass, !isRelated);
      });
      wrapper.querySelectorAll('[data-cfx-role="topology-node"]').forEach(node => {
        const isSelf = detail.kind === 'node' && attr(node, 'data-node-id') === detail.id;
        const isRelated = isSelf || relatedNodes.has(attr(node, 'data-node-id'));
        node.classList.toggle(relatedClass, isRelated);
        node.classList.toggle(mutedClass, !isRelated && detail.kind !== 'group');
      });
      wrapper.querySelectorAll('[data-cfx-role="topology-group"]').forEach(group => {
        const isSelf = detail.kind === 'group' && attr(group, 'data-group-id') === detail.id;
        const isRelated = isSelf || relatedGroups.has(attr(group, 'data-group-id'));
        group.classList.toggle(relatedClass, isRelated);
      });
      if (detail.element && selfClass) detail.element.classList.add(selfClass);
    };
    const hover = element => {
      const detail = identity(element);
      detail.related = related(detail);
      clearHover();
      wrapper.setAttribute('data-cfx-hover-kind', detail.kind);
      wrapper.setAttribute('data-cfx-hover-id', detail.id);
      applyRelatedClasses(detail, 'cfx-topology-html-hover-', 'cfx-topology-html-hovered');
      applyForceFocusLabels(detail);
      delete detail.element;
      wrapper.dispatchEvent(new CustomEvent('cfx-topology-hover', { bubbles: true, detail }));
    };
    const select = (element, emit = true, sync = true) => {
      const detail = identity(element);
      detail.related = related(detail);
      clear();
      wrapper.setAttribute('data-cfx-selection-kind', detail.kind);
      wrapper.setAttribute('data-cfx-selection-id', detail.id);
      wrapper.setAttribute('data-cfx-selection-status', detail.status);
      element.classList.add('cfx-topology-html-selected');
      element.setAttribute('aria-selected', 'true');
      applyRelatedClasses({ ...detail, element }, 'cfx-topology-html-', '');
      applyForceFocusLabels(detail);
      renderSelectionPanel({ ...detail, element });
      delete detail.element;
      if (emit) wrapper.dispatchEvent(new CustomEvent('cfx-topology-select', { bubbles: true, detail }));
      if (sync) emitSync('selection', { selection: { kind: detail.kind, id: detail.id, status: detail.status } });
    };
    const forceSearchText = element => [
      attr(element, 'data-node-id'), attr(element, 'data-node-label'), attr(element, 'data-node-kind'), attr(element, 'data-group-id'),
      attr(element, 'data-edge-id'), attr(element, 'data-edge-label'), attr(element, 'data-edge-secondary-label'), attr(element, 'data-edge-tertiary-label'), attr(element, 'data-edge-kind'), attr(element, 'data-source-node-id'), attr(element, 'data-target-node-id'),
      attr(element, 'data-group-label'), attr(element, 'data-group-symbol'), element.textContent || ''
    ].join(' ').toLowerCase();
    const cssEscape = value => window.CSS && CSS.escape ? CSS.escape(value) : String(value).replace(/["\\]/g, '\\$&');
    const forceGraphState = () => ({
      query: ((forceGraphPanel && forceGraphPanel.querySelector('[data-cfx-force-search]')) || {}).value || '',
      status: ((forceGraphPanel && forceGraphPanel.querySelector('[data-cfx-force-status]')) || {}).value || '',
      group: ((forceGraphPanel && forceGraphPanel.querySelector('[data-cfx-force-group]')) || {}).value || '',
      edges: !!(forceGraphPanel && forceGraphPanel.querySelector('[data-cfx-force-toggle="edges"]:checked')),
      focus: !!(forceGraphPanel && forceGraphPanel.querySelector('[data-cfx-force-toggle="focus"]:checked')),
      labels: !!(forceGraphPanel && forceGraphPanel.querySelector('[data-cfx-force-toggle="edge-labels"]:checked')),
      groups: !!(forceGraphPanel && forceGraphPanel.querySelector('[data-cfx-force-toggle="groups"]:checked')),
      hideMovingEdges: !!(forceGraphPanel && forceGraphPanel.querySelector('[data-cfx-force-toggle="hide-moving-edges"]:checked'))
    });
    const setForceHidden = (selector, predicate) => {
      wrapper.querySelectorAll(selector).forEach(element => element.classList.toggle('cfx-topology-html-force-hidden', predicate(element)));
    };
    const applyForceGraphFilters = () => {
      if (!forceGraphControls || !forceGraphPanel) return;
      const state = forceGraphState();
      const query = state.query.trim().toLowerCase();
      const visibleNodes = new Set();
      const queryNodes = new Set();
      const statusNodes = new Set();
      if (query) {
        wrapper.querySelectorAll('[data-cfx-role="topology-edge"]').forEach(edge => {
          if (!forceSearchText(edge).includes(query)) return;
          queryNodes.add(attr(edge, 'data-source-node-id'));
          queryNodes.add(attr(edge, 'data-target-node-id'));
        });
        wrapper.querySelectorAll('[data-cfx-role="topology-group"]').forEach(group => {
          if (!forceSearchText(group).includes(query)) return;
          const groupId = attr(group, 'data-group-id');
          wrapper.querySelectorAll('[data-cfx-role="topology-node"][data-group-id="' + cssEscape(groupId) + '"]').forEach(node => queryNodes.add(attr(node, 'data-node-id')));
        });
      }
      if (state.status) {
        wrapper.querySelectorAll('[data-cfx-role="topology-edge"]').forEach(edge => {
          if (attr(edge, 'data-cfx-status') !== state.status) return;
          statusNodes.add(attr(edge, 'data-source-node-id'));
          statusNodes.add(attr(edge, 'data-target-node-id'));
        });
      }
      wrapper.setAttribute('data-cfx-force-edges-visible', state.edges ? 'true' : 'false');
      wrapper.setAttribute('data-cfx-force-focus', state.focus ? 'true' : 'false');
      wrapper.setAttribute('data-cfx-force-edge-labels-visible', state.labels ? 'true' : 'false');
      wrapper.setAttribute('data-cfx-force-groups-visible', state.groups ? 'true' : 'false');
      wrapper.setAttribute('data-cfx-force-hide-moving-edges', state.hideMovingEdges ? 'true' : 'false');
      wrapper.querySelectorAll('[data-cfx-role="topology-node"]').forEach(node => {
        const groupOk = !state.group || attr(node, 'data-group-id') === state.group;
        const statusOk = !state.status || attr(node, 'data-cfx-status') === state.status || statusNodes.has(attr(node, 'data-node-id'));
        const queryOk = !query || forceSearchText(node).includes(query) || queryNodes.has(attr(node, 'data-node-id'));
        const visible = groupOk && statusOk && queryOk;
        node.classList.toggle('cfx-topology-html-force-hidden', !visible);
        wrapper.querySelectorAll('[data-cfx-role="topology-node-status"][data-node-id="' + cssEscape(attr(node, 'data-node-id')) + '"]').forEach(status => status.classList.toggle('cfx-topology-html-force-hidden', !visible));
        if (visible) visibleNodes.add(attr(node, 'data-node-id'));
      });
      const nodeById = id => wrapper.querySelector('[data-cfx-role="topology-node"][data-node-id="' + cssEscape(id) + '"]');
      setForceHidden('[data-cfx-role="topology-edge"]', edge => {
        const sourceId = attr(edge, 'data-source-node-id');
        const targetId = attr(edge, 'data-target-node-id');
        const source = nodeById(sourceId);
        const target = nodeById(targetId);
        const endpointsVisible = visibleNodes.has(sourceId) && visibleNodes.has(targetId);
        const edgeQueryOk = !query || forceSearchText(edge).includes(query) || (source && forceSearchText(source).includes(query)) || (target && forceSearchText(target).includes(query));
        const edgeStatusOk = !state.status || attr(edge, 'data-cfx-status') === state.status || (source && attr(source, 'data-cfx-status') === state.status) || (target && attr(target, 'data-cfx-status') === state.status);
        return !state.edges || !endpointsVisible || !edgeQueryOk || !edgeStatusOk;
      });
      setForceHidden('[data-cfx-role="topology-edge-label"]', label => !state.labels || !state.edges || !wrapper.querySelector('[data-cfx-role="topology-edge"][data-edge-id="' + cssEscape(attr(label, 'data-edge-id')) + '"]:not(.cfx-topology-html-force-hidden)'));
      setForceHidden('[data-cfx-role="topology-group"]', group => {
        const groupId = attr(group, 'data-group-id');
        const hasVisibleNodes = !!wrapper.querySelector('[data-cfx-role="topology-node"][data-group-id="' + cssEscape(groupId) + '"]:not(.cfx-topology-html-force-hidden)');
        return !state.groups || !hasVisibleNodes || (state.group && groupId !== state.group);
      });
      const nodeCount = visibleNodes.size;
      const edgeCount = wrapper.querySelectorAll('[data-cfx-role="topology-edge"]:not(.cfx-topology-html-force-hidden)').length;
      const summary = forceGraphPanel.querySelector('[data-cfx-force-summary]');
      if (summary) summary.textContent = nodeCount + ' nodes / ' + edgeCount + ' edges visible';
      wrapper.dispatchEvent(new CustomEvent('cfx-topology-force-filter', { bubbles: true, detail: { chartId: attr(wrapper, 'data-chart-id'), nodes: nodeCount, edges: edgeCount, query: state.query, status: state.status, group: state.group } }));
      restoreForceFocusLabels();
    };
    const clearForceFocusLabels = () => {
      if (!forceGraphControls) return;
      const state = forceGraphPanel ? forceGraphState() : { labels: false };
      wrapper.querySelectorAll('.cfx-topology-html-force-focus-label').forEach(label => {
        label.classList.remove('cfx-topology-html-force-focus-label');
        if (!state.labels) label.classList.add('cfx-topology-html-force-hidden');
      });
    };
    const selectedFocusElement = () => {
      const kind = wrapper.getAttribute('data-cfx-selection-kind') || '';
      const id = wrapper.getAttribute('data-cfx-selection-id') || '';
      return kind && id ? findSelection({ kind, id }) : null;
    };
    const restoreForceFocusLabels = () => {
      if (!forceGraphControls || !forceGraphPanel) return;
      const selected = selectedFocusElement();
      if (selected) applyForceFocusLabels(identity(selected));
      else clearForceFocusLabels();
    };
    const applyForceFocusLabels = detail => {
      if (!forceGraphControls || !forceGraphPanel) return;
      const state = forceGraphState();
      if (!state.focus || !state.edges) {
        clearForceFocusLabels();
        return;
      }
      detail.related = detail.related || related(detail);
      const edgeIds = new Set(detail.related.edgeIds || []);
      if (detail.kind === 'edge') edgeIds.add(detail.id);
      wrapper.querySelectorAll('[data-cfx-role="topology-edge-label"]').forEach(label => {
        const edgeId = attr(label, 'data-edge-id');
        const edge = wrapper.querySelector('[data-cfx-role="topology-edge"][data-edge-id="' + cssEscape(edgeId) + '"]:not(.cfx-topology-html-force-hidden)');
        const active = edgeIds.has(edgeId) && !!edge;
        label.classList.toggle('cfx-topology-html-force-focus-label', active);
        if (active) label.classList.remove('cfx-topology-html-force-hidden');
        else if (!state.labels) label.classList.add('cfx-topology-html-force-hidden');
      });
      const summary = forceGraphPanel.querySelector('[data-cfx-force-summary]');
      if (summary && detail.kind === 'node') summary.textContent = detail.id + ': ' + (detail.related.nodeIds || []).length + ' neighbors / ' + edgeIds.size + ' edges';
    };
    wrapper.querySelectorAll(selectables).forEach(element => {
      if (!element.hasAttribute('tabindex')) element.setAttribute('tabindex', '0');
      if (!element.hasAttribute('role')) element.setAttribute('role', 'button');
      element.addEventListener('pointerenter', () => hover(element));
      element.addEventListener('pointerleave', () => {
        clearHover();
        wrapper.dispatchEvent(new CustomEvent('cfx-topology-hover-clear', { bubbles: true }));
      });
      element.addEventListener('focus', () => hover(element));
      element.addEventListener('blur', () => {
        clearHover();
        wrapper.dispatchEvent(new CustomEvent('cfx-topology-hover-clear', { bubbles: true }));
      });
    });
    const initiallySelected = Array.from(wrapper.querySelectorAll(selectables)).find(element => attr(element, 'data-cfx-selected') === 'true');
    if (initiallySelected) select(initiallySelected, false, false);
    else renderSelectionPanel(null);
    if (viewportControls) {
      applyViewport(viewportState());
      wrapper.querySelectorAll('[data-cfx-topology-zoom]').forEach(button => {
        button.addEventListener('click', event => zoomBy(attr(button, 'data-cfx-topology-zoom') === 'in' ? 1.2 : 0.8333333333, event));
      });
      wrapper.querySelectorAll('[data-cfx-topology-mode]').forEach(button => {
        button.addEventListener('click', () => setViewportMode(attr(button, 'data-cfx-topology-mode')));
      });
      wrapper.querySelectorAll('[data-cfx-topology-fit]').forEach(button => {
        button.addEventListener('click', () => fitViewport());
      });
      wrapper.querySelectorAll('[data-cfx-topology-reset]').forEach(button => {
        button.addEventListener('click', () => resetViewport());
      });
      let drag = null;
      let suppressClick = false;
      const isViewportChrome = target => target instanceof Element && target.closest('.cfx-topology-controls,.cfx-topology-scenarios,.cfx-topology-scenario-panel,.cfx-topology-selection-panel,.cfx-topology-force-controls');
      viewport.addEventListener('wheel', event => {
        if (isViewportChrome(event.target)) return;
        event.preventDefault();
        zoomBy(event.deltaY < 0 ? 1.08 : 0.92, event);
      }, { passive: false });
      viewport.addEventListener('pointerdown', event => {
        if (event.button !== 0) return;
        if (isViewportChrome(event.target)) return;
        const state = viewportState();
        drag = { id: event.pointerId, x: event.clientX, y: event.clientY, panX: state.panX, panY: state.panY, moved: false };
        wrapper.setAttribute('data-cfx-topology-dragging', 'true');
        try { if (viewport.setPointerCapture) viewport.setPointerCapture(event.pointerId); } catch { }
      });
      viewport.addEventListener('pointermove', event => {
        if (!drag || drag.id !== event.pointerId) return;
        const dx = event.clientX - drag.x;
        const dy = event.clientY - drag.y;
        if (Math.abs(dx) > 2 || Math.abs(dy) > 2) {
          drag.moved = true;
          suppressClick = true;
          markForceGraphMoving();
        }
        applyViewport({ zoom: viewportState().zoom, panX: drag.panX + dx, panY: drag.panY + dy });
      });
      viewport.addEventListener('pointerup', event => {
        if (!drag || drag.id !== event.pointerId) return;
        try { if (viewport.releasePointerCapture) viewport.releasePointerCapture(event.pointerId); } catch { }
        if (drag.moved) emitViewport();
        wrapper.removeAttribute('data-cfx-force-moving-edges');
        wrapper.removeAttribute('data-cfx-topology-dragging');
        wrapper.querySelectorAll('.cfx-topology-html-force-moving').forEach(edge => edge.classList.remove('cfx-topology-html-force-moving'));
        drag = null;
      });
      viewport.addEventListener('pointercancel', event => {
        if (drag && drag.id === event.pointerId) {
          wrapper.removeAttribute('data-cfx-topology-dragging');
          drag = null;
        }
      });
      wrapper.addEventListener('cfx-topology-set-viewport', event => {
        applyViewport(event.detail || {});
        emitViewport();
      });
      wrapper.addEventListener('cfx-topology-reset-viewport', () => resetViewport());
      wrapper.addEventListener('click', event => {
        if (isViewportChrome(event.target)) return;
        if (!suppressClick) return;
        suppressClick = false;
        event.preventDefault();
        event.stopPropagation();
      }, true);
    }
    if (exportControls) {
      wrapper.querySelectorAll('[data-cfx-topology-export]').forEach(button => {
        button.addEventListener('click', () => {
          if (attr(button, 'data-cfx-topology-export') === 'png') exportPng();
          else exportSvg();
        });
      });
    }
    if (fullscreenControl) {
      wrapper.querySelectorAll('[data-cfx-topology-fullscreen]').forEach(button => {
        button.addEventListener('click', () => toggleFullscreen());
      });
      document.addEventListener('fullscreenchange', setFullscreenState);
      setFullscreenState();
    }
    if (forceGraphControls && forceGraphPanel) {
      forceGraphPanel.querySelectorAll('input,select').forEach(control => {
        control.addEventListener('input', applyForceGraphFilters);
        control.addEventListener('change', applyForceGraphFilters);
      });
      wrapper.addEventListener('cfx-topology-force-filter-set', event => {
        const detail = event.detail || {};
        if (forceGraphPanel.querySelector('[data-cfx-force-search]') && detail.query !== undefined) forceGraphPanel.querySelector('[data-cfx-force-search]').value = detail.query || '';
        if (forceGraphPanel.querySelector('[data-cfx-force-status]') && detail.status !== undefined) forceGraphPanel.querySelector('[data-cfx-force-status]').value = detail.status || '';
        if (forceGraphPanel.querySelector('[data-cfx-force-group]') && detail.group !== undefined) forceGraphPanel.querySelector('[data-cfx-force-group]').value = detail.group || '';
        applyForceGraphFilters();
      });
      applyForceGraphFilters();
    }
    if (scenarioControls) {
      if (scenarioControlMode === 'checkboxes') {
        wrapper.querySelectorAll('[data-cfx-topology-scenario-toggle]').forEach(input => {
          const scenarioId = attr(input, 'data-cfx-topology-scenario-toggle');
          input.addEventListener('change', () => setScenarioFilters(Array.from(wrapper.querySelectorAll('[data-cfx-topology-scenario-toggle]:checked')).map(item => attr(item, 'data-cfx-topology-scenario-toggle'))));
          input.addEventListener('pointerenter', () => previewScenario(scenarioId));
          input.addEventListener('pointerleave', clearScenarioPreview);
          input.addEventListener('focus', () => previewScenario(scenarioId));
          input.addEventListener('blur', clearScenarioPreview);
        });
      } else {
        wrapper.querySelectorAll('[data-cfx-topology-scenario]').forEach(button => {
          const scenarioId = attr(button, 'data-cfx-topology-scenario');
          button.addEventListener('click', () => setScenario(scenarioId));
          button.addEventListener('pointerenter', () => previewScenario(scenarioId));
          button.addEventListener('pointerleave', clearScenarioPreview);
          button.addEventListener('focus', () => previewScenario(scenarioId));
          button.addEventListener('blur', clearScenarioPreview);
        });
      }
      wrapper.addEventListener('cfx-topology-set-scenario', event => setScenario((event.detail || {}).scenarioId || ''));
      wrapper.addEventListener('cfx-topology-set-scenario-filters', event => setScenarioFilters((event.detail || {}).scenarioIds || []));
      wrapper.addEventListener('cfx-topology-clear-scenario', () => setScenario(''));
      wrapper.addEventListener('cfx-topology-set-scenario-step', event => setScenarioStep((event.detail || {}).index, false));
      wrapper.querySelectorAll('[data-cfx-scenario-step-control]').forEach(button => {
        button.addEventListener('click', () => {
          const action = attr(button, 'data-cfx-scenario-step-control');
          if (action === 'previous') stepScenario(-1);
          else if (action === 'next') stepScenario(1);
          else if (action === 'play') playScenario();
          else if (action === 'reset') { stopScenarioPlayback(); clearScenarioStep(); }
          else if (action === 'link') copyScenarioLink();
        });
      });
      const stepsList = scenarioPanel ? scenarioPanel.querySelector('[data-cfx-scenario-panel-steps]') : null;
      if (stepsList) {
        const focusStep = target => { const item = target instanceof Element ? target.closest('[data-cfx-scenario-step-index]') : null; if (item && stepsList.contains(item)) setScenarioStep(Number(attr(item, 'data-cfx-scenario-step-index')) - 1, false); };
        stepsList.addEventListener('click', event => focusStep(event.target));
        stepsList.addEventListener('keydown', event => { if (event.key !== 'Enter' && event.key !== ' ') return; event.preventDefault(); focusStep(event.target); });
      }
      const initialScenario = scenarioUrlParam('scenario') || attr(wrapper, 'data-cfx-active-scenario');
      const initialScenarioStep = scenarioUrlParam('scenarioStep');
      if (scenarioControlMode === 'checkboxes') setScenarioFilters(initialScenario ? scenarioIdTokens(initialScenario) : Array.from(wrapper.querySelectorAll('[data-cfx-topology-scenario-toggle]:checked')).map(item => attr(item, 'data-cfx-topology-scenario-toggle')), false, false);
      else setScenario(initialScenario, false, false);
      if (initialScenarioStep && scenarioControlMode !== 'checkboxes') setScenarioStep(initialScenarioStep, false, false, false);
    }
    wrapper.addEventListener('click', event => {
      const element = event.target instanceof Element ? event.target.closest(selectables) : null;
      if (!element || !wrapper.contains(element)) return;
      event.preventDefault();
      select(element);
    });
    wrapper.addEventListener('keydown', event => {
      if (event.key === 'Escape') {
        clear();
        wrapper.dispatchEvent(new CustomEvent('cfx-topology-clear', { bubbles: true }));
        emitSync('clear-selection', {});
        return;
      }
      if (event.key === 'ArrowRight' || event.key === 'ArrowDown' || event.key === 'ArrowLeft' || event.key === 'ArrowUp') {
        const element = event.target instanceof Element ? event.target.closest(selectables) : null;
        if (!element || !wrapper.contains(element)) return;
        if (focusRelated(element, event.key === 'ArrowRight' || event.key === 'ArrowDown' ? 1 : -1)) event.preventDefault();
        return;
      }
      if (event.key !== 'Enter' && event.key !== ' ') return;
      const element = event.target instanceof Element ? event.target.closest(selectables) : null;
      if (!element || !wrapper.contains(element)) return;
      event.preventDefault();
      select(element);
    });
    wrapper.addEventListener('cfx-topology-set-selection', event => {
      const element = findSelection(event.detail);
      if (element) select(element);
    });
    wrapper.addEventListener('cfx-topology-clear-selection', () => {
      clear();
      wrapper.dispatchEvent(new CustomEvent('cfx-topology-clear', { bubbles: true }));
      emitSync('clear-selection', {});
    });
    wrapper.addEventListener('cfx-topology-apply-sync', event => {
      const detail = event.detail || {};
      if (!syncEnabled || !syncGroup || detail.group !== syncGroup) return;
      applyingSync = true;
      try {
        if (detail.action === 'selection' && detail.selection) {
          const element = findSelection(detail.selection);
          if (element) select(element);
        } else if (detail.action === 'clear-selection') {
          clear();
          wrapper.dispatchEvent(new CustomEvent('cfx-topology-clear', { bubbles: true }));
        } else if (detail.action === 'viewport' && detail.state) {
          applyViewport(detail.state);
          wrapper.dispatchEvent(new CustomEvent('cfx-topology-viewport', { bubbles: true, detail: { chartId: attr(wrapper, 'data-chart-id'), state: viewportState(), sourceChartId: detail.chartId } }));
        } else if (detail.action === 'scenario') {
          setScenario(detail.scenarioId || '');
        } else if (detail.action === 'scenario-filters') {
          setScenarioFilters(detail.scenarioIds || []);
        } else if (detail.action === 'scenario-step') {
          if (detail.scenarioId && attr(wrapper, 'data-cfx-active-scenario') !== detail.scenarioId) setScenario(detail.scenarioId);
          setScenarioStep(detail.index, false);
        }
      } finally {
        applyingSync = false;
      }
    });
  }
})();
</script>
""";
}
