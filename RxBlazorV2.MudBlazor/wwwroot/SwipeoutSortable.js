// RxBlazorV2.MudBlazor — Swipeout + Sortable
//
// Gesture algorithms (swipeout: elasticity, velocity-snap, overswipe, swipe-to-delete;
// sortable: midpoint-cross sibling shift, edge auto-scroll, tap-hold activation) are derived
// from Framework7 — https://framework7.io — Copyright (c) 2014 Vladimir Kharlampidi, MIT.
// Cross-list group concept (pull / put / clone) follows the SortableJS / BlazorSortable model.
// Re-implemented here for Blazor with Pointer Events + Touch Events (no jQuery / dom7), and
// licensed under the same MIT terms as the rest of this repository.
//
// One ESM module. Two factories: createSwipeout, createSortable.
// Per-list Coordinator coordinates cross-talk (open swipeouts close on sort start;
// sort is blocked while any swipeout is open).
//
// Public per-instance API (returned to .NET via IJSObjectReference):
//   Swipeout : { open(side), close(), refresh(), dispose() }
//   Sortable : { enable(), disable(), refresh(), dispose() }
//
// .NET callbacks (invoked via DotNetObjectReference.invokeMethodAsync):
//   Swipeout : OnStateChangedAsync(state)            state ∈ "closed" | "left-open" | "right-open"
//   Sortable : OnReorderAsync(from, to)              from ≠ to, both 0-based item indices
//
// Overswipe/Delete decoupling: instead of passing commands through JS, the user marks one
// child action element with [data-swipeout-overswipe="true"] (and optionally [data-swipeout-delete="true"]).
// On overswipe release JS dispatches a click on the marked button — Blazor's normal click
// handling fires the command through MudIconButton[Async]Rx, with confirm dialogs and all.

// =============================================================================
// Tunables (match F7 swipeout where they exist; values in CSS px or px/ms)
// =============================================================================
const DRAG_THRESHOLD = 5;
const ELASTICITY = 0.4;
const VELOCITY_SNAP = 0.3;
const OVERSWIPE_EXTRA = 60;
// Width/translate the visual jumps by when overswipe arms — animated over ARMED_ANIM_MS via rAF
// with an easeOutBack overshoot so the auto-widen feels like a self-completing further swipe.
const ARMED_BONUS = 30;
const ARMED_ANIM_MS = 220;
const SNAP_MS = 200;
const DELETE_COLLAPSE_MS = 220;
const TAPHOLD_MS = 500;
const TAP_HOLD_CANCEL_PX = 15;
const EDGE_AUTOSCROLL_PX = 44;
const EDGE_AUTOSCROLL_SPEED = 8;

const reduceMotion = () => window.matchMedia?.("(prefers-reduced-motion: reduce)").matches === true;

// =============================================================================
// Coordinator: one per list-container element. Found via WeakMap.
// =============================================================================
const coordinators = new WeakMap();

class Coordinator
{
    constructor()
    {
        this.openSwipeouts = new Set();
        this.sortingActive = false;
        // Gesture lock: when a swipeout or sortable claims a pointer, the other engine
        // observes this and bails out for the rest of the gesture. Set on threshold-cross,
        // cleared on pointerup/cancel by the engine that set it.
        this.gestureLock = null; // null | "swipeout" | "sortable"
    }

    registerOpen(swipeout)
    {
        this.openSwipeouts.add(swipeout);
    }

    registerClosed(swipeout)
    {
        this.openSwipeouts.delete(swipeout);
    }

    closeAll(except)
    {
        for (const s of [...this.openSwipeouts])
        {
            if (s !== except)
            {
                s.close();
            }
        }
    }

    anyOpen()
    {
        return this.openSwipeouts.size > 0;
    }

    setSorting(v)
    {
        this.sortingActive = v;
    }

    isSorting()
    {
        return this.sortingActive;
    }
}

function getCoordinator(el)
{
    if (el === null || el === undefined)
    {
        return new Coordinator();
    }
    let c = coordinators.get(el);
    if (c === undefined)
    {
        c = new Coordinator();
        coordinators.set(el, c);
    }
    return c;
}

// =============================================================================
// Helpers
// =============================================================================
function findScrollableAncestor(el)
{
    let cur = el.parentElement;
    while (cur !== null && cur !== document.body)
    {
        const cs = window.getComputedStyle(cur);
        const oy = cs.overflowY;
        if ((oy === "auto" || oy === "scroll") && cur.scrollHeight > cur.clientHeight)
        {
            return cur;
        }
        cur = cur.parentElement;
    }
    return document.scrollingElement ?? document.documentElement;
}

function setTransition(el, ms)
{
    if (ms <= 0 || reduceMotion() === true)
    {
        el.style.transition = "none";
    }
    else
    {
        el.style.transition = `transform ${ms}ms cubic-bezier(0.25, 0.46, 0.45, 0.94)`;
    }
}

function clearInlineTransitions(els)
{
    for (const el of els)
    {
        el.style.transition = "";
    }
}

// =============================================================================
// Swipeout
// =============================================================================
export function createSwipeout(rowEl, dotnetRef, opts)
{
    const listContainer = rowEl.closest("[data-rxb-list]") ?? rowEl.parentElement;
    const coord = getCoordinator(listContainer);

    const left = rowEl.querySelector(":scope > [data-swipeout-actions=\"left\"]");
    const right = rowEl.querySelector(":scope > [data-swipeout-actions=\"right\"]");
    const content = rowEl.querySelector(":scope > [data-swipeout-content]");
    if (content === null)
    {
        throw new Error("MudSwipeoutRx: missing [data-swipeout-content] element");
    }

    function findMarked(panel, attr)
    {
        if (panel === null)
        {
            return null;
        }
        return panel.querySelector(`[${attr}="true"]`);
    }

    function dispatchActionClick(panel)
    {
        const marker = findMarked(panel, "data-swipeout-overswipe");
        if (marker === null)
        {
            return;
        }
        // Blazor renders MudIconButton as a <button>. Click the inner clickable.
        const btn = marker.querySelector("button, a") ?? marker;
        btn.click();
    }

    function hasOverswipe(panel)
    {
        return findMarked(panel, "data-swipeout-overswipe") !== null;
    }

    function hasNonTrigger(panel)
    {
        if (panel === null)
        {
            return false;
        }
        return panel.querySelector('[data-swipeout-action]:not([data-swipeout-overswipe="true"])') !== null;
    }

    function hasDelete(panel)
    {
        return findMarked(panel, "data-swipeout-delete") !== null;
    }

    let leftWidth = 0;
    let rightWidth = 0;

    let state = "closed";
    let openedOffset = 0;
    let isDragging = false;
    let pointerId = null;
    let touchId = null;
    let startX = 0;
    let startY = 0;
    let lastX = 0;
    let lastT = 0;
    let velocity = 0;
    let currentOffset = 0;
    let overswipeArmed = null;

    // Armed-bonus animation: when armed, leftBonus / rightBonus interpolate 0 → ARMED_BONUS via
    // rAF, with easeOutBack overshoot. Visual offset = drag offset ± bonus, applied per frame so
    // the bonus composes with continuous drag without CSS-transition lag.
    let leftBonus = 0;
    let rightBonus = 0;
    let leftBonusAnim = null;
    let rightBonusAnim = null;
    let bonusRaf = 0;

    // Decision-hold state: when an action carries data-swipeout-confirm the row stays at its
    // current visual offset until .NET resolves the user's choice via notifyActionDecided. This
    // keeps the row swept-open / fully-swept across while a confirmation dialog is showing.
    let actionDecisionResolver = null;

    const instance = {};

    function measure()
    {
        // Strip any inline width so we read each panel's natural action-content width.
        if (left !== null)
        {
            left.style.width = "";
            leftWidth = left.scrollWidth;
        }
        if (right !== null)
        {
            right.style.width = "";
            rightWidth = right.scrollWidth;
        }
    }

    function panelTransition(panel, ms)
    {
        if (ms <= 0 || reduceMotion() === true)
        {
            panel.style.transition = "none";
        }
        else
        {
            panel.style.transition = `width ${ms}ms cubic-bezier(0.25, 0.46, 0.45, 0.94)`;
        }
    }

    function easeOutBack(t)
    {
        // Spring-overshoot easing: peaks above 1 around t≈0.7 then settles at 1.
        const c1 = 1.70158;
        const c3 = c1 + 1;
        return 1 + c3 * Math.pow(t - 1, 3) + c1 * Math.pow(t - 1, 2);
    }

    function setArmedBonusTarget(side, target)
    {
        const currentValue = side === "left" ? leftBonus : rightBonus;
        const currentAnim = side === "left" ? leftBonusAnim : rightBonusAnim;
        // Already at or animating to target — no-op.
        if (currentAnim !== null && currentAnim.target === target)
        {
            return;
        }
        if (currentAnim === null && currentValue === target)
        {
            return;
        }

        // prefers-reduced-motion: snap directly to the target instead of running the spring rAF.
        // Visuals refresh once via applyVisuals so the new bonus value takes effect immediately.
        if (reduceMotion() === true)
        {
            if (side === "left")
            {
                leftBonus = target;
                leftBonusAnim = null;
            }
            else
            {
                rightBonus = target;
                rightBonusAnim = null;
            }
            applyVisuals();
            return;
        }

        // Start (or replace) the spring animation for this side from its current value to target.
        const anim = {
            from: currentValue,
            target: target,
            startTime: performance.now()
        };
        if (side === "left")
        {
            leftBonusAnim = anim;
        }
        else
        {
            rightBonusAnim = anim;
        }
        if (bonusRaf === 0)
        {
            bonusRaf = requestAnimationFrame(bonusRafTick);
        }
    }

    function bonusRafTick(now)
    {
        let stillAnimating = false;
        if (leftBonusAnim !== null)
        {
            const t = Math.min(1, (now - leftBonusAnim.startTime) / ARMED_ANIM_MS);
            const eased = easeOutBack(t);
            leftBonus = leftBonusAnim.from + (leftBonusAnim.target - leftBonusAnim.from) * eased;
            if (t < 1)
            {
                stillAnimating = true;
            }
            else
            {
                leftBonus = leftBonusAnim.target;
                leftBonusAnim = null;
            }
        }
        if (rightBonusAnim !== null)
        {
            const t = Math.min(1, (now - rightBonusAnim.startTime) / ARMED_ANIM_MS);
            const eased = easeOutBack(t);
            rightBonus = rightBonusAnim.from + (rightBonusAnim.target - rightBonusAnim.from) * eased;
            if (t < 1)
            {
                stillAnimating = true;
            }
            else
            {
                rightBonus = rightBonusAnim.target;
                rightBonusAnim = null;
            }
        }
        applyVisuals();
        if (stillAnimating === true)
        {
            bonusRaf = requestAnimationFrame(bonusRafTick);
        }
        else
        {
            bonusRaf = 0;
        }
    }

    function applyVisuals()
    {
        // Visual offset combines the gesture's offset with any in-flight armed bonus.
        let visualOffset = currentOffset;
        if (currentOffset > 0)
        {
            visualOffset = currentOffset + leftBonus;
        }
        else if (currentOffset < 0)
        {
            visualOffset = currentOffset - rightBonus;
        }

        // Symmetric motion: content slides in both directions so the entire row moves with the
        // gesture. Right panel sits z-index:0 (below content) so the slide-left reveals it.
        content.style.transform = `translate3d(${visualOffset}px, 0, 0)`;

        if (left !== null)
        {
            let w = leftWidth;
            if (visualOffset > 0)
            {
                w = Math.max(leftWidth, visualOffset);
            }
            left.style.width = `${w}px`;
            applyButtonReveal(left, visualOffset > 0 ? visualOffset : 0, false);
        }
        if (right !== null)
        {
            const w = visualOffset < 0 ? -visualOffset : 0;
            right.style.width = `${w}px`;
            applyButtonReveal(right, w, true);
        }
    }

    /**
     * Per-button reveal progress (0..1) for the iOS-style scale-in effect. CSS reads
     * --rxb-reveal on each [data-swipeout-action] and applies transform: scale(var(...)) when
     * the row carries the rxb-reveal-scale class. Without that class the var is harmless — the
     * default CSS keeps scale(1) regardless.
     *
     * Reveal order:
     *   - Left panel (flex-start): buttons appear left → right (DOM order).
     *   - Right panel (flex-end):  buttons appear right → left (rightmost = first).
     *
     * Each button gets its own progress: starts at 0 when the panel is too narrow to show it,
     * reaches 1 when fully revealed at the panel's natural width.
     */
    function applyButtonReveal(panel, visibleWidth, rightToLeft)
    {
        const buttons = panel.querySelectorAll(":scope > [data-swipeout-action]");
        const n = buttons.length;
        if (n === 0)
        {
            return;
        }
        const buttonWidth = readActionWidth(buttons[0]);
        for (let i = 0; i < n; i++)
        {
            const positionFromRevealing = rightToLeft === true ? (n - 1 - i) : i;
            const startW = positionFromRevealing * buttonWidth;
            const progress = Math.max(0, Math.min(1, (visibleWidth - startW) / buttonWidth));
            buttons[i].style.setProperty("--rxb-reveal", String(progress));
        }
    }

    function readActionWidth(actionEl)
    {
        const cs = window.getComputedStyle(actionEl);
        const basis = parseFloat(cs.flexBasis);
        return Number.isNaN(basis) === true ? 64 : basis;
    }

    function applyOffset(offset, animate)
    {
        currentOffset = offset;
        const ms = animate === true ? SNAP_MS : 0;

        // Set transitions for snap (open / close / fire). During drag (animate=false) the
        // transitions are cleared so per-frame width / translate updates are instant.
        setTransition(content, ms);
        if (left !== null)
        {
            panelTransition(left, ms);
        }
        if (right !== null)
        {
            panelTransition(right, ms);
        }

        // Determine armed state from the raw gesture offset (the bonus is purely visual; arming
        // depends on the user's finger crossing the threshold).
        let nextArmed = null;
        if (offset > 0 && offset >= leftWidth + OVERSWIPE_EXTRA && left !== null && hasOverswipe(left) === true)
        {
            nextArmed = "left";
        }
        else if (offset < 0 && -offset >= rightWidth + OVERSWIPE_EXTRA && right !== null && hasOverswipe(right) === true)
        {
            nextArmed = "right";
        }

        // Bonus auto-widen only fires for multi-button rows. Single-button rows use the icon-shift
        // CSS rule alone as the armed-state signal (matches the chosen design — iOS does this for
        // delete and the panel widen would otherwise look redundant when there's only one action).
        const leftBonusTarget = (nextArmed === "left" && hasNonTrigger(left)) ? ARMED_BONUS : 0;
        const rightBonusTarget = (nextArmed === "right" && hasNonTrigger(right)) ? ARMED_BONUS : 0;

        // Drive the armed-bonus animation:
        //   - During drag (animate=false): rAF interpolates 0 ↔ ARMED_BONUS with overshoot.
        //   - During snap (animate=true): set bonus instantly to target, cancel rAF — the snap's
        //     own transition handles the visual change in lockstep with width / translate.
        if (animate === true)
        {
            leftBonus = leftBonusTarget;
            rightBonus = rightBonusTarget;
            leftBonusAnim = null;
            rightBonusAnim = null;
            if (bonusRaf !== 0)
            {
                cancelAnimationFrame(bonusRaf);
                bonusRaf = 0;
            }
        }
        else
        {
            setArmedBonusTarget("left", leftBonusTarget);
            setArmedBonusTarget("right", rightBonusTarget);
        }

        // Apply visuals using current offset + (possibly in-flight) bonus.
        applyVisuals();

        if (left !== null)
        {
            left.classList.toggle("rxb-overswipe-armed", nextArmed === "left");
        }
        if (right !== null)
        {
            right.classList.toggle("rxb-overswipe-armed", nextArmed === "right");
        }

        overswipeArmed = nextArmed;
    }

    function setState(newState)
    {
        if (state === newState)
        {
            return;
        }
        state = newState;
        if (state === "closed")
        {
            coord.registerClosed(instance);
            rowEl.classList.remove("rxb-open");
        }
        else
        {
            coord.registerOpen(instance);
            rowEl.classList.add("rxb-open");
        }
        dotnetRef.invokeMethodAsync("OnStateChangedAsync", state);
    }

    function snapTo(target, animate)
    {
        applyOffset(target, animate);
        openedOffset = target;
        if (target === 0)
        {
            setState("closed");
        }
        else if (target > 0)
        {
            setState("left-open");
        }
        else
        {
            setState("right-open");
        }
    }

    async function fireOuterAction(side, isDelete)
    {
        // Animate the full row sweep — applyOffset to a target far past the threshold drives
        // overswipe progress to 1, which fully expands the trigger via CSS so the user sees a
        // single full-width action firing.
        const panel = side === "left" ? left : right;
        const rowWidth = rowEl.getBoundingClientRect().width;
        const target = side === "left" ? rowWidth : -rowWidth;
        applyOffset(target, true);
        await new Promise(r => setTimeout(r, SNAP_MS));

        // If the outer action has a confirmation wired, set up a decision promise BEFORE
        // dispatching the click so the row stays at its current swept-open visual until the
        // user's choice resolves. .NET calls notifyActionDecided(ok) once the dialog returns.
        const marker = findMarked(panel, "data-swipeout-overswipe");
        const hasConfirm = marker !== null && marker.getAttribute("data-swipeout-confirm") === "true";
        let decisionPromise = null;
        if (hasConfirm === true)
        {
            decisionPromise = new Promise(r => { actionDecisionResolver = r; });
        }

        dispatchActionClick(panel);

        if (decisionPromise !== null)
        {
            await decisionPromise;
        }

        if (isDelete === true)
        {
            // Yield two frames so Blazor can process the click + re-render.
            await new Promise(r => requestAnimationFrame(() => requestAnimationFrame(r)));
            if (rowEl.isConnected === false)
            {
                return;
            }
        }
        // Either non-delete action (snap closed after firing — onActionClick would do the same on a tap),
        // or delete cancelled (e.g. ConfirmExecutionAsync returned false): snap closed.
        snapTo(0, true);
    }

    function performDelete()
    {
        return fireOuterAction("right", true);
    }

    function performOverswipe(side)
    {
        return fireOuterAction(side, false);
    }

    /**
     * Resolve the offset for a given finger displacement and apply it.
     *
     * Pre-arm the offset is elastic (rubber-band feel past natural width). Past the overswipe
     * threshold the offset becomes linear with finger movement so further drag grows the panel
     * 1:1 — both formulas agree at the exact arm boundary so the transition is smooth.
     * applyOffset is responsible for translating the offset into panel width + overswipe progress
     * + armed state.
     */
    function applySwipeMove(dx)
    {
        // 1. Elastic candidate offset (rubber band past natural width).
        let offset = openedOffset + dx;
        if (offset > leftWidth)
        {
            offset = leftWidth + (offset - leftWidth) * ELASTICITY;
        }
        if (offset < -rightWidth)
        {
            offset = -rightWidth + (offset + rightWidth) * ELASTICITY;
        }

        // 2. Past the arm threshold, switch to a linear formula so further finger motion grows
        //    the panel 1:1. dxAtArm is the finger position at which the elastic formula reaches
        //    the threshold; the linear formula starts from there.
        if (hasOverswipe(right) === true && offset < -(rightWidth + OVERSWIPE_EXTRA))
        {
            const dxAtArm = -OVERSWIPE_EXTRA / ELASTICITY - rightWidth - openedOffset;
            offset = -rightWidth - OVERSWIPE_EXTRA + (dx - dxAtArm);
        }
        else if (hasOverswipe(left) === true && offset > leftWidth + OVERSWIPE_EXTRA)
        {
            const dxAtArm = OVERSWIPE_EXTRA / ELASTICITY + leftWidth - openedOffset;
            offset = leftWidth + OVERSWIPE_EXTRA + (dx - dxAtArm);
        }

        // 3. Block movement to a side that has no actions.
        if (left === null && offset > 0)
        {
            offset = 0;
        }
        if (right === null && offset < 0)
        {
            offset = 0;
        }

        applyOffset(offset, false);
    }

    function onPointerDown(e)
    {
        // Touch input is handled by the parallel touch-event path (passive:false touchmove gets
        // preventDefault on iOS where pointer events are unreliable). Pointer events drive mouse/pen.
        if (e.pointerType === "touch")
        {
            return;
        }
        if (e.pointerType === "mouse" && e.button !== 0)
        {
            return;
        }
        if (coord.isSorting() === true)
        {
            return;
        }
        // Don't capture taps on action buttons — let them click normally.
        if (e.target.closest("[data-swipeout-action]") !== null)
        {
            return;
        }
        // Don't capture pointer-down on a sort handle — that's the sortable's territory,
        // even if the user moves horizontally afterwards (cross-list drag).
        if (e.target.closest("[data-rxb-sort-handle]") !== null)
        {
            return;
        }
        // If already-open and tap is on content, close instead of dragging.
        if (state !== "closed" && e.target.closest("[data-swipeout-content]") !== null)
        {
            // Defer: only close if no drag occurs.
        }
        pointerId = e.pointerId;
        startX = e.clientX;
        startY = e.clientY;
        lastX = startX;
        lastT = performance.now();
        velocity = 0;
        isDragging = false;
    }

    function onPointerMove(e)
    {
        if (e.pointerId !== pointerId)
        {
            return;
        }
        const dx = e.clientX - startX;
        const dy = e.clientY - startY;
        if (isDragging === false)
        {
            if (Math.abs(dx) < DRAG_THRESHOLD && Math.abs(dy) < DRAG_THRESHOLD)
            {
                return;
            }
            // Vertical wins -> abandon (sortable territory).
            if (Math.abs(dy) > Math.abs(dx))
            {
                pointerId = null;
                return;
            }
            // If sortable has fully claimed, abandon. If only tentatively holding (tap-hold pending),
            // defer this move — keep pointerId so we can claim once sortable cancels its tentative
            // lock (e.g. user moved past TAP_HOLD_CANCEL_PX without holding still long enough).
            if (coord.gestureLock === "sortable")
            {
                pointerId = null;
                return;
            }
            if (coord.gestureLock === "sortable-pending")
            {
                return;
            }
            isDragging = true;
            coord.gestureLock = "swipeout";
            measure();
            try
            {
                rowEl.setPointerCapture(pointerId);
            }
            catch (_)
            {
                // Some browsers throw if pointer already captured; safe to ignore.
            }
            rowEl.classList.add("rxb-dragging");
            coord.closeAll(instance);
        }

        e.preventDefault();
        const t = performance.now();
        const dt = Math.max(1, t - lastT);
        velocity = (e.clientX - lastX) / dt;
        lastX = e.clientX;
        lastT = t;

        applySwipeMove(dx);
    }

    function onPointerUp(e)
    {
        if (e.pointerId !== pointerId)
        {
            return;
        }
        const wasDragging = isDragging;
        pointerId = null;
        isDragging = false;
        rowEl.classList.remove("rxb-dragging");
        if (coord.gestureLock === "swipeout")
        {
            coord.gestureLock = null;
        }
        try
        {
            rowEl.releasePointerCapture(e.pointerId);
        }
        catch (_)
        {
            // Already released or never captured.
        }

        if (wasDragging === false)
        {
            // No drag happened. If swipeout is open and tap was on content, close it.
            if (state !== "closed" && e.target?.closest("[data-swipeout-content]") !== null)
            {
                snapTo(0, true);
            }
            return;
        }

        // Delete-overswipe (right side) takes precedence if marked.
        if (overswipeArmed === "right" && hasDelete(right) === true)
        {
            overswipeArmed = null;
            performDelete();
            return;
        }
        if (overswipeArmed !== null)
        {
            const side = overswipeArmed;
            overswipeArmed = null;
            performOverswipe(side);
            return;
        }

        const offset = currentOffset;
        const flickRight = velocity > VELOCITY_SNAP;
        const flickLeft = velocity < -VELOCITY_SNAP;

        let target;
        if (flickRight === true && left !== null)
        {
            target = leftWidth;
        }
        else if (flickLeft === true && right !== null)
        {
            target = -rightWidth;
        }
        else if (offset > leftWidth / 2 && left !== null)
        {
            target = leftWidth;
        }
        else if (offset < -rightWidth / 2 && right !== null)
        {
            target = -rightWidth;
        }
        else
        {
            target = 0;
        }
        snapTo(target, true);
    }

    function onClickOutside(e)
    {
        if (state === "closed")
        {
            return;
        }
        if (rowEl.contains(e.target) === true)
        {
            return;
        }
        snapTo(0, true);
    }

    function onActionClick(e)
    {
        // Close after the user taps any action — matches iOS / F7 default. Overswipe and delete
        // are handled separately (they call snapTo themselves).
        if (state === "closed")
        {
            return;
        }
        const action = e.target.closest("[data-swipeout-action]");
        if (action === null)
        {
            return;
        }
        // If the action has a confirmation wired, hold the row open at its current swept state
        // until .NET signals the decision via notifyActionDecided. Otherwise snap closed after
        // a one-frame defer (gives the click handler time to run before the spring-back).
        if (action.getAttribute("data-swipeout-confirm") === "true")
        {
            actionDecisionResolver = () => snapTo(0, true);
            return;
        }
        requestAnimationFrame(() => snapTo(0, true));
    }

    // ----- Touch event path (iOS / Android touch input) ---------------------------------------
    // Pointer events on iOS Safari are unreliable for early-threshold gestures (browser may fire
    // pointercancel before our 5px disambiguation runs). Touch events with passive:false let us
    // call preventDefault on every touchmove and own the gesture from frame one — this is the
    // F7-proven path and what we rely on for touch input. Pointer events drive mouse/pen above.

    function findTouch(e, id)
    {
        for (const t of e.touches)
        {
            if (t.identifier === id)
            {
                return t;
            }
        }
        for (const t of e.changedTouches)
        {
            if (t.identifier === id)
            {
                return t;
            }
        }
        return null;
    }

    function onTouchStart(e)
    {
        if (e.touches.length !== 1)
        {
            return;
        }
        if (touchId !== null || pointerId !== null)
        {
            return;
        }
        if (coord.isSorting() === true)
        {
            return;
        }
        const t = e.touches[0];
        const target = t.target;
        if (target.closest("[data-swipeout-action]") !== null)
        {
            return;
        }
        if (target.closest("[data-rxb-sort-handle]") !== null)
        {
            return;
        }
        touchId = t.identifier;
        startX = t.clientX;
        startY = t.clientY;
        lastX = startX;
        lastT = performance.now();
        velocity = 0;
        isDragging = false;
    }

    function onTouchMove(e)
    {
        if (touchId === null)
        {
            return;
        }
        const t = findTouch(e, touchId);
        if (t === null)
        {
            return;
        }
        // Always claim — we own gestures inside a row on touch.
        if (e.cancelable === true)
        {
            e.preventDefault();
        }

        const dx = t.clientX - startX;
        const dy = t.clientY - startY;
        if (isDragging === false)
        {
            if (Math.abs(dx) < DRAG_THRESHOLD && Math.abs(dy) < DRAG_THRESHOLD)
            {
                return;
            }
            if (Math.abs(dy) > Math.abs(dx))
            {
                touchId = null;
                return;
            }
            if (coord.gestureLock === "sortable")
            {
                touchId = null;
                return;
            }
            if (coord.gestureLock === "sortable-pending")
            {
                return;
            }
            isDragging = true;
            coord.gestureLock = "swipeout";
            measure();
            rowEl.classList.add("rxb-dragging");
            coord.closeAll(instance);
        }

        const tNow = performance.now();
        const dt = Math.max(1, tNow - lastT);
        velocity = (t.clientX - lastX) / dt;
        lastX = t.clientX;
        lastT = tNow;

        applySwipeMove(dx);
    }

    function onTouchEnd(e)
    {
        if (touchId === null)
        {
            return;
        }
        const t = findTouch(e, touchId);
        const wasDragging = isDragging;
        const tapTarget = t !== null ? t.target : null;
        touchId = null;
        isDragging = false;
        rowEl.classList.remove("rxb-dragging");
        if (coord.gestureLock === "swipeout")
        {
            coord.gestureLock = null;
        }

        if (wasDragging === false)
        {
            if (state !== "closed" && tapTarget !== null && tapTarget.closest("[data-swipeout-content]") !== null)
            {
                snapTo(0, true);
            }
            return;
        }

        if (overswipeArmed === "right" && hasDelete(right) === true)
        {
            overswipeArmed = null;
            performDelete();
            return;
        }
        if (overswipeArmed !== null)
        {
            const side = overswipeArmed;
            overswipeArmed = null;
            performOverswipe(side);
            return;
        }

        const offset = currentOffset;
        const flickRight = velocity > VELOCITY_SNAP;
        const flickLeft = velocity < -VELOCITY_SNAP;
        let target;
        if (flickRight === true && left !== null)
        {
            target = leftWidth;
        }
        else if (flickLeft === true && right !== null)
        {
            target = -rightWidth;
        }
        else if (offset > leftWidth / 2 && left !== null)
        {
            target = leftWidth;
        }
        else if (offset < -rightWidth / 2 && right !== null)
        {
            target = -rightWidth;
        }
        else
        {
            target = 0;
        }
        snapTo(target, true);
    }

    function onTouchCancel()
    {
        if (touchId === null)
        {
            return;
        }
        touchId = null;
        const wasDragging = isDragging;
        isDragging = false;
        rowEl.classList.remove("rxb-dragging");
        if (coord.gestureLock === "swipeout")
        {
            coord.gestureLock = null;
        }
        if (wasDragging === true)
        {
            snapTo(openedOffset === 0 ? 0 : openedOffset, true);
        }
    }

    // Pointer events drive mouse / pen.
    rowEl.addEventListener("pointerdown", onPointerDown);
    rowEl.addEventListener("pointermove", onPointerMove);
    rowEl.addEventListener("pointerup", onPointerUp);
    rowEl.addEventListener("pointercancel", onPointerUp);
    rowEl.addEventListener("lostpointercapture", onPointerUp);
    rowEl.addEventListener("click", onActionClick);
    // Touch events drive finger input — touchmove must be non-passive so preventDefault claims
    // the gesture against the browser's scroll heuristic on iOS Safari + Android Chrome, where
    // pointer-event preventDefault alone isn't reliable.
    rowEl.addEventListener("touchstart", onTouchStart);
    rowEl.addEventListener("touchmove", onTouchMove, { passive: false });
    rowEl.addEventListener("touchend", onTouchEnd);
    rowEl.addEventListener("touchcancel", onTouchCancel);
    document.addEventListener("pointerdown", onClickOutside, true);

    measure();
    applyOffset(0, false);

    instance.open = function (side)
    {
        measure();
        if (side === "left" && left !== null)
        {
            snapTo(leftWidth, true);
        }
        else if (side === "right" && right !== null)
        {
            snapTo(-rightWidth, true);
        }
    };
    instance.close = function ()
    {
        snapTo(0, true);
    };
    instance.refresh = function ()
    {
        measure();
        applyOffset(openedOffset, false);
    };
    /**
     * Called from .NET after a confirmation gate (ConfirmExecutionAsync) resolves. Releases the
     * row hold set up by fireOuterAction / onActionClick: a tap path snaps closed; a swipe path's
     * pending decisionPromise resolves so its post-click flow continues. The `ok` argument is
     * forwarded for symmetry but the row's snap behaviour is the same either way (delete removes
     * the item; non-delete just closes).
     */
    instance.notifyActionDecided = function (ok)
    {
        if (actionDecisionResolver === null)
        {
            return;
        }
        const r = actionDecisionResolver;
        actionDecisionResolver = null;
        r(ok);
    };
    instance.dispose = function ()
    {
        rowEl.removeEventListener("pointerdown", onPointerDown);
        rowEl.removeEventListener("pointermove", onPointerMove);
        rowEl.removeEventListener("pointerup", onPointerUp);
        rowEl.removeEventListener("pointercancel", onPointerUp);
        rowEl.removeEventListener("lostpointercapture", onPointerUp);
        rowEl.removeEventListener("click", onActionClick);
        rowEl.removeEventListener("touchstart", onTouchStart);
        rowEl.removeEventListener("touchmove", onTouchMove);
        rowEl.removeEventListener("touchend", onTouchEnd);
        rowEl.removeEventListener("touchcancel", onTouchCancel);
        document.removeEventListener("pointerdown", onClickOutside, true);
        coord.registerClosed(instance);
        clearInlineTransitions([content, left, right].filter(x => x !== null));
        if (content !== null)
        {
            content.style.transform = "";
        }
        if (left !== null)
        {
            left.style.width = "";
        }
        if (right !== null)
        {
            right.style.width = "";
        }
    };
    return instance;
}

// =============================================================================
// Sortable — cross-list capable
// =============================================================================
//
// Module-level group registry: instances declaring the same group.name can exchange items.
const sortableGroups = new Map(); // groupName -> Set<instance>

function registerInGroup(groupName, instance)
{
    if (groupName === null || groupName === undefined)
    {
        return;
    }
    let set = sortableGroups.get(groupName);
    if (set === undefined)
    {
        set = new Set();
        sortableGroups.set(groupName, set);
    }
    set.add(instance);
}

function unregisterFromGroup(groupName, instance)
{
    if (groupName === null || groupName === undefined)
    {
        return;
    }
    const set = sortableGroups.get(groupName);
    if (set !== undefined)
    {
        set.delete(instance);
        if (set.size === 0)
        {
            sortableGroups.delete(groupName);
        }
    }
}

function findTargetForPointer(srcInstance, clientX, clientY)
{
    // Source list always wins if pointer is inside its bounds.
    if (pointerInside(srcInstance.listEl, clientX, clientY) === true)
    {
        return srcInstance;
    }
    if (srcInstance.groupName === null || srcInstance.groupName === undefined)
    {
        // Isolated list — drag-out-to-remove is only opted-in via cross-list groups, so an
        // outside pointer just falls back to source (no-op on release).
        return srcInstance;
    }
    const candidates = sortableGroups.get(srcInstance.groupName);
    if (candidates !== undefined)
    {
        for (const cand of candidates)
        {
            if (cand === srcInstance)
            {
                continue;
            }
            if (cand.put !== true)
            {
                continue;
            }
            if (pointerInside(cand.listEl, clientX, clientY) === true)
            {
                return cand;
            }
        }
    }
    // Grouped + no put-capable target under pointer = drag-out (drop will remove if pull === "move").
    return null;
}

function pointerInside(el, clientX, clientY)
{
    const r = el.getBoundingClientRect();
    return clientX >= r.left && clientX <= r.right && clientY >= r.top && clientY <= r.bottom;
}

export function createSortable(listEl, dotnetRef, opts)
{
    const coord = getCoordinator(listEl);
    let activation = opts.activation ?? "drag-handle"; // "drag-handle" | "tap-hold" | "always"
    const listId = opts.listId ?? "";
    const groupName = opts.groupName ?? null;
    const pull = opts.pull ?? "move"; // "none" | "move" | "clone"
    const put = opts.put === true;
    let enabled = opts.enabled !== false;

    let pointerId = null;
    let touchId = null;
    let startX = 0;
    let startY = 0;
    let pointerOffsetX = 0;
    let pointerOffsetY = 0;
    let dragEl = null;
    let dragElIndex = -1;
    let dragElHeight = 0;
    let dragElWidth = 0;
    let dragElCollapsed = false;
    let cloneEl = null;
    let activeTarget = null; // SortableInstance currently being dropped into; null = outside any target
    let activeSiblings = [];
    let activeInsertBefore = -1;
    let activeInsertAfter = -1;
    let scrollAncestor = null;
    let scrollStart = 0;
    let isDragging = false;
    let tapHoldTimer = null;
    let autoScrollRaf = 0;
    let lastClientX = 0;
    let lastClientY = 0;

    const instance = {};

    function items(el)
    {
        return [...el.querySelectorAll(":scope > [data-rxb-sortable-item]")];
    }

    function localItems()
    {
        return items(listEl);
    }

    function findRow(target)
    {
        const row = target.closest("[data-rxb-sortable-item]");
        if (row === null || row.parentElement !== listEl)
        {
            return null;
        }
        return row;
    }

    function activationOk(e)
    {
        if (activation === "drag-handle")
        {
            return e.target.closest("[data-rxb-sort-handle]") !== null;
        }
        if (activation === "always")
        {
            // Any pointer-down on a row is eligible — vertical-vs-horizontal disambiguation is
            // handled by the threshold races against the swipeout (which owns the X-axis).
            return true;
        }
        return false;
    }

    function makeClone(srcRect)
    {
        const c = dragEl.cloneNode(true);
        c.classList.add("rxb-drag-ghost");
        c.style.position = "fixed";
        c.style.left = `${srcRect.left}px`;
        c.style.top = `${srcRect.top}px`;
        c.style.width = `${srcRect.width}px`;
        c.style.height = `${srcRect.height}px`;
        c.style.pointerEvents = "none";
        c.style.zIndex = "10000";
        c.style.opacity = "0.92";
        c.style.transform = "none";
        c.style.transition = "none";
        document.body.appendChild(c);
        return c;
    }

    function moveCloneTo(clientX, clientY)
    {
        if (cloneEl === null)
        {
            return;
        }
        cloneEl.style.left = `${clientX - pointerOffsetX}px`;
        cloneEl.style.top = `${clientY - pointerOffsetY}px`;
    }

    function clearActiveTargetVisuals()
    {
        if (activeTarget === null)
        {
            return;
        }
        for (const s of activeSiblings)
        {
            s.style.transition = "";
            s.style.transform = "";
        }
        activeTarget.listEl.classList.remove("rxb-sorting-active");
        activeTarget.listEl.classList.remove("rxb-drop-target");
        activeTarget.listEl.style.removeProperty("--rxb-drag-slot-height");
        activeTarget = null;
        activeSiblings = [];
        activeInsertBefore = -1;
        activeInsertAfter = -1;
    }

    function setDragElCollapsed(collapse)
    {
        if (dragEl === null || dragElCollapsed === collapse)
        {
            return;
        }
        dragElCollapsed = collapse;
        const ms = reduceMotion() === true ? 0 : 200;
        if (collapse === true)
        {
            // Pin natural height so the transition has a starting value.
            dragEl.style.height = `${dragElHeight}px`;
            dragEl.style.overflow = "hidden";
            void dragEl.getBoundingClientRect();
            dragEl.style.transition = `height ${ms}ms cubic-bezier(0.25, 0.46, 0.45, 0.94)`;
            dragEl.style.height = "0px";
        }
        else
        {
            dragEl.style.transition = `height ${ms}ms cubic-bezier(0.25, 0.46, 0.45, 0.94)`;
            dragEl.style.height = `${dragElHeight}px`;
        }
    }

    function setActiveTarget(target)
    {
        if (activeTarget === target)
        {
            return;
        }
        clearActiveTargetVisuals();

        if (target === null)
        {
            // Drag is outside any valid target — collapse the source slot so the source list
            // visually shrinks; on release we'll emit a remove (when source.pull === "move").
            setDragElCollapsed(true);
            // Mark clone as "removing" so the user sees it's not landing anywhere.
            if (cloneEl !== null && pull === "move")
            {
                cloneEl.classList.add("rxb-drag-ghost-removing");
            }
            return;
        }

        if (cloneEl !== null)
        {
            cloneEl.classList.remove("rxb-drag-ghost-removing");
        }

        activeTarget = target;
        activeSiblings = items(target.listEl).filter(el => el !== dragEl);
        target.listEl.classList.add("rxb-sorting-active");

        if (target === instance)
        {
            // Back over source — restore its slot.
            setDragElCollapsed(false);
        }
        else
        {
            // Cross-list target — collapse source slot, expand target by drag-slot height.
            setDragElCollapsed(true);
            target.listEl.classList.add("rxb-drop-target");
            target.listEl.style.setProperty("--rxb-drag-slot-height", `${dragElHeight}px`);
        }
    }

    function beginDrag(e, row)
    {
        if (coord.gestureLock === "swipeout")
        {
            return false;
        }
        if (pull === "none")
        {
            return false;
        }
        if (coord.anyOpen() === true)
        {
            coord.closeAll(null);
        }
        coord.setSorting(true);
        coord.gestureLock = "sortable";
        dragEl = row;
        dragElIndex = localItems().indexOf(dragEl);
        const rect = dragEl.getBoundingClientRect();
        dragElHeight = rect.height;
        dragElWidth = rect.width;
        pointerOffsetX = e.clientX - rect.left;
        pointerOffsetY = e.clientY - rect.top;
        scrollAncestor = findScrollableAncestor(listEl);
        scrollStart = scrollAncestor.scrollTop;
        startY = e.clientY;
        lastClientX = e.clientX;
        lastClientY = e.clientY;
        isDragging = true;

        cloneEl = makeClone(rect);
        dragEl.classList.add("rxb-sorting");
        // Hide the original in-flow row so the clone (following the pointer) is the only visible copy.
        // We keep it in flow initially so the source list's height stays the same — no re-flow.
        // setDragElCollapsed(true) on cross-list / outside transitions later will animate the slot
        // closing for visual feedback.
        dragEl.style.opacity = "0";
        dragElCollapsed = false;

        setActiveTarget(instance);
        try
        {
            listEl.setPointerCapture(pointerId);
        }
        catch (_)
        {
            // ignore
        }
        return true;
    }

    function updateDragVisuals(clientX, clientY)
    {
        moveCloneTo(clientX, clientY);

        // Decide active target based on pointer position. May be null when pointer is outside
        // any valid put-target (drag-out-to-remove state).
        const target = findTargetForPointer(instance, clientX, clientY);
        setActiveTarget(target);

        if (activeTarget === null)
        {
            // No siblings to shift — clone is following the pointer "in space", source slot
            // is collapsing; drop here = remove (when pull === "move").
            return;
        }

        const tgtListEl = activeTarget.listEl;
        const tgtRect = tgtListEl.getBoundingClientRect();
        const sa = findScrollableAncestor(tgtListEl);
        const tgtScrollDelta = sa.scrollTop - scrollStart;

        // Pointer's Y in target list's coordinate space (offsetTop space).
        const pointerInTarget = clientY - tgtRect.top + tgtListEl.scrollTop;
        const draggedCenter = pointerInTarget;

        let nextBefore = -1;
        let nextAfter = -1;

        for (let i = 0; i < activeSiblings.length; i++)
        {
            const sib = activeSiblings[i];
            const sibTop = sib.offsetTop;
            const sibHeight = sib.getBoundingClientRect().height;
            const sibCenter = sibTop + sibHeight / 2;
            const sibIndex = items(tgtListEl).indexOf(sib);

            let translate = 0;
            const isCrossList = activeTarget !== instance;

            if (isCrossList === true)
            {
                // dragEl isn't in this list — just open a gap below the crossed midpoint.
                if (draggedCenter < sibCenter)
                {
                    translate = dragElHeight;
                    if (nextBefore === -1)
                    {
                        nextBefore = sibIndex;
                    }
                }
            }
            else
            {
                if (sibIndex > dragElIndex && draggedCenter > sibCenter)
                {
                    translate = -dragElHeight;
                    nextAfter = sibIndex;
                    nextBefore = -1;
                }
                else if (sibIndex < dragElIndex && draggedCenter < sibCenter)
                {
                    translate = dragElHeight;
                    if (nextBefore === -1)
                    {
                        nextBefore = sibIndex;
                    }
                }
            }

            if (sib.style.transition === "")
            {
                sib.style.transition = reduceMotion() === true
                    ? "none"
                    : `transform ${SNAP_MS}ms cubic-bezier(0.25, 0.46, 0.45, 0.94)`;
            }
            sib.style.transform = translate === 0 ? "" : `translate3d(0, ${translate}px, 0)`;
        }

        activeInsertBefore = nextBefore;
        activeInsertAfter = nextAfter;
    }

    function endDrag(commit)
    {
        if (autoScrollRaf !== 0)
        {
            cancelAnimationFrame(autoScrollRaf);
            autoScrollRaf = 0;
        }
        if (dragEl === null)
        {
            return;
        }

        const target = activeTarget; // may be null (drag-out-to-remove)
        const isOutside = target === null;
        const isCrossList = isOutside === false && target !== instance;
        const from = dragElIndex;

        // Compute final to-index in target list space.
        let toIndex = -1;
        if (isOutside === false)
        {
            if (isCrossList === true)
            {
                if (activeInsertBefore !== -1)
                {
                    toIndex = activeInsertBefore;
                }
                else if (activeInsertAfter !== -1)
                {
                    toIndex = activeInsertAfter + 1;
                }
                else
                {
                    toIndex = items(target.listEl).length;
                }
            }
            else
            {
                if (activeInsertBefore !== -1)
                {
                    toIndex = activeInsertBefore > from ? activeInsertBefore - 1 : activeInsertBefore;
                }
                else if (activeInsertAfter !== -1)
                {
                    toIndex = activeInsertAfter < from ? activeInsertAfter + 1 : activeInsertAfter;
                }
                else
                {
                    toIndex = from;
                }
            }
        }

        // Reset visuals (no animation — we're about to re-render via .NET).
        for (const s of activeSiblings)
        {
            s.style.transition = "";
            s.style.transform = "";
        }
        if (cloneEl !== null)
        {
            cloneEl.remove();
            cloneEl = null;
        }
        dragEl.classList.remove("rxb-sorting");
        dragEl.style.opacity = "";
        dragEl.style.transform = "";
        dragEl.style.transition = "";
        dragEl.style.height = "";
        dragEl.style.overflow = "";
        listEl.classList.remove("rxb-sorting-active");
        listEl.classList.remove("rxb-drop-target");
        listEl.style.removeProperty("--rxb-drag-slot-height");
        if (activeTarget !== null)
        {
            activeTarget.listEl.classList.remove("rxb-sorting-active");
            activeTarget.listEl.classList.remove("rxb-drop-target");
            activeTarget.listEl.style.removeProperty("--rxb-drag-slot-height");
        }

        const targetListId = isOutside === true ? "" : target.listId;
        const isClone = isCrossList === true && pull === "clone";
        const isRemove = isOutside === true && pull === "move";

        // Final state cleanup.
        coord.setSorting(false);
        if (coord.gestureLock === "sortable")
        {
            coord.gestureLock = null;
        }
        dragEl = null;
        activeTarget = null;
        activeSiblings = [];
        activeInsertBefore = -1;
        activeInsertAfter = -1;
        isDragging = false;
        dragElCollapsed = false;

        if (commit !== true)
        {
            return;
        }

        // Skip no-op intra-list drops (released at original index) and outside-drops on a clone source
        // (clone source has nothing to remove).
        if (isCrossList === false && isOutside === false && toIndex === from)
        {
            return;
        }
        if (isOutside === true && isRemove === false)
        {
            return;
        }

        dotnetRef.invokeMethodAsync(
            "OnReorderAsync",
            listId,
            from,
            targetListId,
            toIndex,
            isClone,
            isRemove);
    }

    function autoScrollTick()
    {
        if (isDragging === false)
        {
            autoScrollRaf = 0;
            return;
        }
        const sa = scrollAncestor;
        const rect = sa === document.scrollingElement || sa === document.documentElement
            ? { top: 0, bottom: window.innerHeight }
            : sa.getBoundingClientRect();
        let dy = 0;
        if (lastClientY < rect.top + EDGE_AUTOSCROLL_PX)
        {
            dy = -EDGE_AUTOSCROLL_SPEED;
        }
        else if (lastClientY > rect.bottom - EDGE_AUTOSCROLL_PX)
        {
            dy = EDGE_AUTOSCROLL_SPEED;
        }
        if (dy !== 0)
        {
            sa.scrollTop += dy;
            updateDragVisuals(lastClientX, lastClientY);
        }
        autoScrollRaf = requestAnimationFrame(autoScrollTick);
    }

    function clearTapHold()
    {
        if (tapHoldTimer !== null)
        {
            clearTimeout(tapHoldTimer);
            tapHoldTimer = null;
        }
    }

    function onPointerDown(e)
    {
        if (enabled === false)
        {
            return;
        }
        // Touch input is handled by the parallel touch-event path below.
        if (e.pointerType === "touch")
        {
            return;
        }
        if (e.pointerType === "mouse" && e.button !== 0)
        {
            return;
        }
        // Pointer-down on an action button — let it click normally; never start a drag.
        if (e.target.closest("[data-swipeout-action]") !== null)
        {
            return;
        }
        const row = findRow(e.target);
        if (row === null)
        {
            return;
        }
        pointerId = e.pointerId;
        startX = e.clientX;
        startY = e.clientY;
        lastClientX = e.clientX;
        lastClientY = e.clientY;

        if (activation === "tap-hold")
        {
            // Tentatively claim the gesture so swipeout doesn't grab small horizontal drift during
            // the hold window. Cleared on cancel, upgraded to "sortable" by beginDrag on timer fire.
            if (coord.gestureLock === null)
            {
                coord.gestureLock = "sortable-pending";
            }
            tapHoldTimer = setTimeout(() =>
            {
                tapHoldTimer = null;
                if (pointerId === e.pointerId)
                {
                    if (beginDrag(e, row) !== true)
                    {
                        pointerId = null;
                        if (coord.gestureLock === "sortable-pending")
                        {
                            coord.gestureLock = null;
                        }
                    }
                }
            }, TAPHOLD_MS);
            return;
        }
        if (activationOk(e) === false)
        {
            return;
        }
    }

    function onPointerMove(e)
    {
        if (e.pointerId !== pointerId)
        {
            return;
        }
        lastClientX = e.clientX;
        lastClientY = e.clientY;
        if (isDragging === false)
        {
            const dx = e.clientX - startX;
            const dy = e.clientY - startY;
            const moved = Math.max(Math.abs(dx), Math.abs(dy));
            if (tapHoldTimer !== null && moved > TAP_HOLD_CANCEL_PX)
            {
                // User moved past the hold-zone before the timer fired — cancel tap-hold and
                // release the pending lock so swipeout can claim a horizontal swipe.
                clearTapHold();
                pointerId = null;
                if (coord.gestureLock === "sortable-pending")
                {
                    coord.gestureLock = null;
                }
                return;
            }
            if (activation !== "tap-hold" && moved >= DRAG_THRESHOLD)
            {
                const row = findRow(e.target);
                if (row === null)
                {
                    pointerId = null;
                    return;
                }
                if (activationOk(e) === false)
                {
                    pointerId = null;
                    return;
                }
                if (beginDrag(e, row) !== true)
                {
                    pointerId = null;
                    return;
                }
            }
            else
            {
                return;
            }
        }
        e.preventDefault();
        updateDragVisuals(e.clientX, e.clientY);
        if (autoScrollRaf === 0)
        {
            autoScrollRaf = requestAnimationFrame(autoScrollTick);
        }
    }

    function onPointerUp(e)
    {
        if (e.pointerId !== pointerId)
        {
            return;
        }
        clearTapHold();
        const wasDragging = isDragging;
        pointerId = null;
        // Released before the tap-hold timer fired — release the tentative lock so the next
        // gesture is unblocked.
        if (wasDragging === false && coord.gestureLock === "sortable-pending")
        {
            coord.gestureLock = null;
        }
        try
        {
            listEl.releasePointerCapture(e.pointerId);
        }
        catch (_)
        {
            // ignore
        }
        if (wasDragging === true)
        {
            endDrag(true);
        }
    }

    function onPointerCancel(e)
    {
        if (e.pointerId !== pointerId)
        {
            return;
        }
        clearTapHold();
        const wasDragging = isDragging;
        pointerId = null;
        if (wasDragging === false && coord.gestureLock === "sortable-pending")
        {
            coord.gestureLock = null;
        }
        if (wasDragging === true)
        {
            endDrag(false);
        }
    }

    // ----- Touch event path (iOS / Android touch input) ---------------------------------------
    function findTouch(e, id)
    {
        for (const t of e.touches)
        {
            if (t.identifier === id)
            {
                return t;
            }
        }
        for (const t of e.changedTouches)
        {
            if (t.identifier === id)
            {
                return t;
            }
        }
        return null;
    }

    function onTouchStart(e)
    {
        if (enabled === false)
        {
            return;
        }
        if (e.touches.length !== 1)
        {
            return;
        }
        if (touchId !== null || pointerId !== null)
        {
            return;
        }
        const t = e.touches[0];
        if (t.target.closest("[data-swipeout-action]") !== null)
        {
            return;
        }
        const row = findRow(t.target);
        if (row === null)
        {
            return;
        }
        touchId = t.identifier;
        startX = t.clientX;
        startY = t.clientY;
        lastClientX = t.clientX;
        lastClientY = t.clientY;

        if (activation === "tap-hold")
        {
            if (coord.gestureLock === null)
            {
                coord.gestureLock = "sortable-pending";
            }
            tapHoldTimer = setTimeout(() =>
            {
                tapHoldTimer = null;
                if (touchId === t.identifier)
                {
                    // Synthesize an event-like object for beginDrag's pointerCapture path —
                    // we don't actually need to capture for touch (the touchId tracking handles it).
                    const fakeEvt = { clientX: lastClientX, clientY: lastClientY, pointerId: -1 };
                    if (beginDrag(fakeEvt, row) !== true)
                    {
                        touchId = null;
                        if (coord.gestureLock === "sortable-pending")
                        {
                            coord.gestureLock = null;
                        }
                    }
                }
            }, TAPHOLD_MS);
            return;
        }
        if (activationOk({ target: t.target }) === false)
        {
            return;
        }
    }

    function onTouchMove(e)
    {
        if (touchId === null)
        {
            return;
        }
        const t = findTouch(e, touchId);
        if (t === null)
        {
            return;
        }
        if (e.cancelable === true)
        {
            e.preventDefault();
        }
        lastClientX = t.clientX;
        lastClientY = t.clientY;
        if (isDragging === false)
        {
            const dx = t.clientX - startX;
            const dy = t.clientY - startY;
            const moved = Math.max(Math.abs(dx), Math.abs(dy));
            if (tapHoldTimer !== null && moved > TAP_HOLD_CANCEL_PX)
            {
                clearTapHold();
                touchId = null;
                if (coord.gestureLock === "sortable-pending")
                {
                    coord.gestureLock = null;
                }
                return;
            }
            if (activation !== "tap-hold" && moved >= DRAG_THRESHOLD)
            {
                const row = findRow(t.target);
                if (row === null)
                {
                    touchId = null;
                    return;
                }
                if (activationOk({ target: t.target }) === false)
                {
                    touchId = null;
                    return;
                }
                const fakeEvt = { clientX: t.clientX, clientY: t.clientY, pointerId: -1 };
                if (beginDrag(fakeEvt, row) !== true)
                {
                    touchId = null;
                    return;
                }
            }
            else
            {
                return;
            }
        }
        updateDragVisuals(t.clientX, t.clientY);
        if (autoScrollRaf === 0)
        {
            autoScrollRaf = requestAnimationFrame(autoScrollTick);
        }
    }

    function onTouchEnd(e)
    {
        if (touchId === null)
        {
            return;
        }
        const t = findTouch(e, touchId);
        if (t === null)
        {
            return;
        }
        clearTapHold();
        const wasDragging = isDragging;
        touchId = null;
        if (wasDragging === false && coord.gestureLock === "sortable-pending")
        {
            coord.gestureLock = null;
        }
        if (wasDragging === true)
        {
            endDrag(true);
        }
    }

    function onTouchCancel(e)
    {
        if (touchId === null)
        {
            return;
        }
        clearTapHold();
        const wasDragging = isDragging;
        touchId = null;
        if (wasDragging === false && coord.gestureLock === "sortable-pending")
        {
            coord.gestureLock = null;
        }
        if (wasDragging === true)
        {
            endDrag(false);
        }
    }

    instance.listEl = listEl;
    instance.listId = listId;
    instance.groupName = groupName;
    instance.pull = pull;
    instance.put = put;

    // Pointer events drive mouse / pen; touch events drive finger input (touchmove non-passive
    // so preventDefault wins the gesture against the browser's scroll heuristic).
    listEl.addEventListener("pointerdown", onPointerDown);
    listEl.addEventListener("pointermove", onPointerMove);
    listEl.addEventListener("pointerup", onPointerUp);
    listEl.addEventListener("pointercancel", onPointerCancel);
    listEl.addEventListener("lostpointercapture", onPointerCancel);
    listEl.addEventListener("touchstart", onTouchStart);
    listEl.addEventListener("touchmove", onTouchMove, { passive: false });
    listEl.addEventListener("touchend", onTouchEnd);
    listEl.addEventListener("touchcancel", onTouchCancel);

    registerInGroup(groupName, instance);

    instance.enable = function () { enabled = true; };
    instance.disable = function () { enabled = false; };
    instance.setActivation = function (mode)
    {
        // Flips between "drag-handle" / "tap-hold" / "always" without re-mounting the instance.
        // Only takes effect on the next gesture; an in-flight drag keeps its current activation.
        activation = mode;
    };
    instance.refresh = function () { /* no internal cache — items() walks DOM */ };
    instance.dispose = function ()
    {
        listEl.removeEventListener("pointerdown", onPointerDown);
        listEl.removeEventListener("pointermove", onPointerMove);
        listEl.removeEventListener("pointerup", onPointerUp);
        listEl.removeEventListener("pointercancel", onPointerCancel);
        listEl.removeEventListener("lostpointercapture", onPointerCancel);
        listEl.removeEventListener("touchstart", onTouchStart);
        listEl.removeEventListener("touchmove", onTouchMove);
        listEl.removeEventListener("touchend", onTouchEnd);
        listEl.removeEventListener("touchcancel", onTouchCancel);
        clearTapHold();
        if (autoScrollRaf !== 0)
        {
            cancelAnimationFrame(autoScrollRaf);
            autoScrollRaf = 0;
        }
        if (cloneEl !== null)
        {
            cloneEl.remove();
            cloneEl = null;
        }
        coord.setSorting(false);
        unregisterFromGroup(groupName, instance);
    };
    return instance;
}
