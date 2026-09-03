(function () {
    'use strict';

    var GB_COLORS = [
        '#2E3192', '#E31E24', '#16a34a', '#d97706', '#0369a1',
        '#7c3aed', '#0891b2', '#be185d', '#4d7c0f', '#c4191f',
        '#232574', '#64748b', '#166534', '#b45309', '#1d4ed8'
    ];

    var charts = {};

    function prop(obj, name) {
        if (!obj) return undefined;
        if (obj[name] !== undefined && obj[name] !== null) return obj[name];
        var pascal = name.charAt(0).toUpperCase() + name.slice(1);
        return obj[pascal];
    }

    function sliceLabels(items) {
        return (items || []).map(function (s) { return prop(s, 'label') || 'Unknown'; });
    }

    function sliceCounts(items) {
        return (items || []).map(function (s) { return prop(s, 'count') || 0; });
    }

    function destroyChart(key) {
        if (charts[key]) {
            charts[key].destroy();
            charts[key] = null;
        }
    }

    function makeDoughnut(canvasId, items) {
        var el = document.getElementById(canvasId);
        if (!el) return;
        destroyChart(canvasId);
        var labels = sliceLabels(items);
        var data = sliceCounts(items);
        if (!labels.length) {
            labels = ['No data'];
            data = [0];
        }
        charts[canvasId] = new Chart(el, {
            type: 'doughnut',
            data: {
                labels: labels,
                datasets: [{
                    data: data,
                    backgroundColor: GB_COLORS.slice(0, labels.length),
                    borderWidth: 1
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: { position: 'bottom', labels: { boxWidth: 12, font: { size: 11 } } }
                }
            }
        });
    }

    function makeBar(canvasId, items, horizontal) {
        var el = document.getElementById(canvasId);
        if (!el) return;
        destroyChart(canvasId);
        var labels = sliceLabels(items);
        var data = sliceCounts(items);
        if (!labels.length) {
            labels = ['No data'];
            data = [0];
        }
        charts[canvasId] = new Chart(el, {
            type: 'bar',
            data: {
                labels: labels,
                datasets: [{
                    label: 'Count',
                    data: data,
                    backgroundColor: '#2E3192',
                    borderRadius: 4
                }]
            },
            options: {
                indexAxis: horizontal ? 'y' : 'x',
                responsive: true,
                maintainAspectRatio: false,
                plugins: { legend: { display: false } },
                scales: {
                    x: { ticks: { font: { size: 10 } } },
                    y: { ticks: { font: { size: 10 } }, beginAtZero: true }
                }
            }
        });
    }

    function makeLineTrends(trends) {
        var el = document.getElementById('chartMonthlyTrends');
        if (!el) return;
        destroyChart('chartMonthlyTrends');
        var items = trends || [];
        var labels = items.map(function (t) { return prop(t, 'label'); });
        if (!labels.length) labels = ['No data'];

        charts['chartMonthlyTrends'] = new Chart(el, {
            type: 'line',
            data: {
                labels: labels,
                datasets: [
                    {
                        label: 'Hires',
                        data: items.map(function (t) { return prop(t, 'hires') || 0; }),
                        borderColor: '#16a34a',
                        backgroundColor: 'rgba(22,163,74,.1)',
                        tension: 0.3,
                        fill: false
                    },
                    {
                        label: 'Separations',
                        data: items.map(function (t) { return prop(t, 'separations') || 0; }),
                        borderColor: '#E31E24',
                        backgroundColor: 'rgba(227,30,36,.1)',
                        tension: 0.3,
                        fill: false
                    },
                    {
                        label: 'Leave Applications',
                        data: items.map(function (t) { return prop(t, 'leaveApplications') || 0; }),
                        borderColor: '#d97706',
                        backgroundColor: 'rgba(217,119,6,.1)',
                        tension: 0.3,
                        fill: false
                    },
                    {
                        label: 'Active Headcount',
                        data: items.map(function (t) { return prop(t, 'headcount') || 0; }),
                        borderColor: '#2E3192',
                        backgroundColor: 'rgba(46,49,146,.08)',
                        tension: 0.3,
                        fill: false
                    }
                ]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: { position: 'bottom', labels: { boxWidth: 12, font: { size: 11 } } }
                },
                scales: {
                    y: { beginAtZero: true, ticks: { font: { size: 10 } } },
                    x: { ticks: { font: { size: 10 }, maxRotation: 45 } }
                }
            }
        });
    }

    function renderCharts(data) {
        if (!data) return;
        makeDoughnut('chartDivision', data.byDivision);
        makeBar('chartDepartment', data.byDepartment, true);
        makeDoughnut('chartRegion', data.byRegion);
        makeDoughnut('chartGender', data.byGender);
        makeDoughnut('chartEmploymentType', data.byEmploymentType);
        makeBar('chartAgeGroup', data.byAgeGroup, false);
        makeBar('chartLeaveCategory', data.byLeaveCategory, true);
        makeLineTrends(data.monthlyTrends);
    }

    function formatKpiValue(key, val) {
        if (val === null || val === undefined) return '—';
        if (key === 'attendanceRate' || key === 'absenteeismRate') {
            return typeof val === 'number' ? val.toFixed(1) : val;
        }
        return val;
    }

    function updateKpis(kpis) {
        if (!kpis) return;
        document.querySelectorAll('[data-kpi]').forEach(function (el) {
            var key = el.getAttribute('data-kpi');
            var val = prop(kpis, key);
            el.textContent = formatKpiValue(key, val);
        });
    }

    function readChartData() {
        var el = document.getElementById('chartData');
        if (!el) return {};
        try {
            return JSON.parse(el.textContent || '{}');
        } catch (e) {
            console.error('Failed to parse chart data', e);
            return {};
        }
    }

    function getFilterParams() {
        var params = new URLSearchParams(window.location.search);
        params.delete('handler');
        return params;
    }

    function refreshDashboard() {
        var btn = document.getElementById('btnRefresh');
        if (btn) {
            btn.disabled = true;
            btn.textContent = 'Refreshing…';
        }

        var params = getFilterParams();
        params.set('handler', 'Refresh');

        fetch(window.location.pathname + '?' + params.toString(), {
            headers: { 'Accept': 'application/json' }
        })
            .then(function (resp) { return resp.json(); })
            .then(function (data) {
                if (data.error) {
                    alert(data.error);
                    return;
                }
                var updated = document.getElementById('dashboardUpdated');
                if (updated && data.generatedAt) updated.textContent = data.generatedAt;
                updateKpis(data.kpis);
                renderCharts(data.chartData);
            })
            .catch(function (err) {
                console.error('Dashboard refresh failed', err);
            })
            .finally(function () {
                if (btn) {
                    btn.disabled = false;
                    btn.textContent = 'Refresh';
                }
            });
    }

    document.addEventListener('DOMContentLoaded', function () {
        renderCharts(readChartData());

        var btn = document.getElementById('btnRefresh');
        if (btn) btn.addEventListener('click', refreshDashboard);
    });
})();
