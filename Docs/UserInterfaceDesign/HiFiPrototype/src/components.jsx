/* global React */
/* eslint-disable no-undef */

// Win95 primitive components — Window, GroupBox, Button, Checkbox, etc.
// All controls follow the styles in styles/win95.css.

const { useState, useEffect, useRef } = React;

// ---------- Window ----------
function W95Window({
  title,
  icon,
  width,
  height,
  inactive = false,
  onClose,
  onMin,
  onMax,
  showMinMax = false,
  showHelp = false,
  helpActive = false,
  onHelpToggle,
  children,
  className = "",
  noPadding = false,
  noTitleIcon = false
}) {
  return (
    <div className={`window window--block ${className}`} style={{ width, height }}>
      <div className={`titlebar ${inactive ? "titlebar--inactive" : ""}`}>
        {!noTitleIcon &&
        <div className="titlebar__icon">
            {icon || <Icons.AppMark size={16} />}
          </div>
        }
        <div className="titlebar__text">{title}</div>
        <div className="titlebar__buttons">
          {showMinMax &&
          <>
              <button className="titlebar__btn" onClick={onMin} aria-label="Minimise" title="Minimise">
                <svg width="6" height="6" viewBox="0 0 6 6"><rect x="0" y="5" width="6" height="1" fill="#000" /></svg>
              </button>
              <button className="titlebar__btn" onClick={onMax} aria-label="Maximise" title="Maximise">
                <svg width="8" height="8" viewBox="0 0 8 8"><rect x="0" y="0" width="8" height="8" fill="none" stroke="#000" /><rect x="0" y="0" width="8" height="2" fill="#000" /></svg>
              </button>
            </>
          }
          {showHelp &&
          <button
            className={`titlebar__btn ${helpActive ? "titlebar__btn--pressed" : ""}`}
            onClick={onHelpToggle}
            aria-label="What's This?"
            title="What's This?"
            style={{ fontWeight: "bold", fontFamily: "var(--ff-ui, sans-serif)" }}>
            ?</button>
          }
          <button className="titlebar__btn" onClick={onClose} aria-label="Close" title="Close" style={{ fontWeight: "bold" }}>×</button>
        </div>
      </div>
      <div style={{ padding: noPadding ? 0 : 0 }}>
        {children}
      </div>
    </div>);

}

// ---------- Button ----------
function Btn({ children, onClick, disabled, pressed, isDefault, sm, xs, wide, style, type = "button", ariaLabel, accel }) {
  return (
    <button
      type={type}
      onClick={onClick}
      disabled={disabled}
      aria-label={ariaLabel}
      className={[
      "btn",
      sm && "btn--sm",
      xs && "btn--xs",
      wide && "btn--wide",
      pressed && "btn--pressed",
      isDefault && "btn--default"].
      filter(Boolean).join(" ")}
      style={{ ...style, justifyContent: "center" }}>
      
      {accel ? renderAccel(children, accel) : children}
    </button>);

}

function renderAccel(label, accel) {
  if (typeof label !== "string") return label;
  const i = label.toLowerCase().indexOf(accel.toLowerCase());
  if (i < 0) return label;
  return <>{label.slice(0, i)}<u>{label[i]}</u>{label.slice(i + 1)}</>;
}

// ---------- GroupBox ----------
function GroupBox({ label, children, style, className = "" }) {
  return (
    <fieldset className={`group ${className}`} style={style}>
      <legend className="group__label">{label}</legend>
      {children}
    </fieldset>);

}

// ---------- Checkbox ----------
function Checkbox({ checked, onChange, disabled, label, id }) {
  const inputId = id || `chk-${Math.random().toString(36).slice(2)}`;
  return (
    <label htmlFor={inputId} className="check">
      <input
        id={inputId}
        type="checkbox"
        checked={!!checked}
        onChange={(e) => onChange && onChange(e.target.checked)}
        disabled={disabled} />
      
      <span className="check__box" />
      <span className="check__label">{label}</span>
    </label>);

}

// ---------- Radio ----------
function Radio({ name, value, current, onChange, label }) {
  return (
    <label className="radio">
      <input
        type="radio"
        name={name}
        checked={current === value}
        onChange={() => onChange && onChange(value)} />
      
      <span className="radio__dot" />
      <span>{label}</span>
    </label>);

}

// ---------- TextField ----------
function TextField({ value, onChange, placeholder, disabled, readOnly, style, width, mono = false, invalid }) {
  return (
    <input
      type="text"
      className={`input ${readOnly ? "input--readonly" : ""} ${invalid ? "input--invalid" : ""} ${mono ? "mono" : ""}`}
      value={value ?? ""}
      placeholder={placeholder}
      disabled={disabled}
      readOnly={readOnly}
      onChange={(e) => onChange && onChange(e.target.value)}
      style={{ width, ...style }} />);


}

// ---------- NumericUpDown ----------
function NumberField({ value, onChange, min = 0, max = 999999, step = 1, width = 80 }) {
  const v = Number.isFinite(value) ? value : 0;
  const set = (n) => onChange && onChange(Math.max(min, Math.min(max, n)));
  return (
    <div className="numupdown" style={{ width }}>
      <input
        className="numupdown__input"
        type="text"
        value={v}
        onChange={(e) => {
          const n = parseInt(e.target.value.replace(/[^-0-9]/g, ""), 10);
          if (!Number.isNaN(n)) set(n);else
          if (e.target.value === "" || e.target.value === "-") onChange && onChange(0);
        }} />
      
      <div className="numupdown__buttons">
        <button className="numupdown__btn numupdown__btn--up" onClick={() => set(v + step)} aria-label="Increase" />
        <button className="numupdown__btn numupdown__btn--down" onClick={() => set(v - step)} aria-label="Decrease" />
      </div>
    </div>);

}

// ---------- ComboBox ----------
function Combo({ value, options, onChange, width = 160, disabled }) {
  const [open, setOpen] = useState(false);
  const ref = useRef(null);

  useEffect(() => {
    function onDoc(e) {
      if (ref.current && !ref.current.contains(e.target)) setOpen(false);
    }
    document.addEventListener("mousedown", onDoc);
    return () => document.removeEventListener("mousedown", onDoc);
  }, []);

  const current = options.find((o) => o.value === value) || options[0];

  return (
    <div className="combo" style={{ width, opacity: disabled ? 0.6 : 1 }} ref={ref} onClick={() => !disabled && setOpen((o) => !o)}>
      <div className="combo__value">{current ? current.label : ""}</div>
      <div className="combo__btn" />
      {open &&
      <div style={{
        position: "absolute",
        top: "100%",
        left: -1, right: -1,
        background: "#ffffff",
        border: "1px solid #000",
        maxHeight: 180,
        overflowY: "auto",
        zIndex: 200
      }}>
          {options.map((o) =>
        <div
          key={o.value}
          style={{
            padding: "3px 6px",
            background: o.value === value ? "#000080" : "transparent",
            color: o.value === value ? "#ffffff" : "#000",
            cursor: "default"
          }}
          onMouseEnter={(e) => {if (o.value !== value) e.currentTarget.style.background = "#dfdfdf";}}
          onMouseLeave={(e) => {if (o.value !== value) e.currentTarget.style.background = "transparent";}}
          onClick={(e) => {e.stopPropagation();onChange && onChange(o.value);setOpen(false);}}>
          
              {o.label}
            </div>
        )}
        </div>
      }
    </div>);

}

// ---------- MenuBar ----------
function MenuBar({ items, onCommand }) {
  const [open, setOpen] = useState(null);
  const closeAll = () => setOpen(null);

  useEffect(() => {
    if (open === null) return;
    const close = () => closeAll();
    document.addEventListener("click", close);
    return () => document.removeEventListener("click", close);
  }, [open]);

  return (
    <div className="menubar">
      {items.map((m) =>
      <div
        key={m.id}
        className={`menubar__item ${open === m.id ? "menubar__item--open" : ""}`}
        onClick={(e) => {e.stopPropagation();setOpen(open === m.id ? null : m.id);}}
        onMouseEnter={() => {if (open !== null && open !== m.id) setOpen(m.id);}}>
        
          {renderAccel(m.label, m.accel || "")}
          {open === m.id &&
        <div className="menu" onClick={(e) => e.stopPropagation()}>
              {m.items.map((it, idx) => {
            if (it.kind === "sep") return <div key={idx} className="menu__sep" />;
            return (
              <div
                key={idx}
                className={`menu__item ${it.disabled ? "menu__item--disabled" : ""}`}
                onClick={() => {
                  if (it.disabled) return;
                  onCommand && onCommand(it.cmd, it);
                  closeAll();
                }}>
                
                    {it.checkable && <span className="menu__check" aria-hidden="true">{it.checked ? "✓" : ""}</span>}
                    {renderAccel(it.label, it.accel || "")}
                    {it.accelText && <span className="menu__accel">{it.accelText}</span>}
                  </div>);

          })}
            </div>
        }
        </div>
      )}
    </div>);

}

// ---------- StatusBar ----------
function StatusBar({ cells }) {
  return (
    <div className="statusbar">
      {cells.map((c, i) =>
      <div
        key={i}
        className={`statusbar__cell ${c.fixed ? "statusbar__cell--fixed" : ""} ${c.className || ""}`}
        style={c.style}>
        
          {c.icon}
          <span>{c.text}</span>
        </div>
      )}
    </div>);

}

// ---------- Tabs ----------
function Tabs({ value, onChange, items, children }) {
  return (
    <div>
      <div className="tabs">
        {items.map((t) =>
        <div
          key={t.id}
          className={`tab ${value === t.id ? "tab--active" : ""}`}
          onClick={() => onChange(t.id)}>
          
            {renderAccel(t.label, t.accel || "")}
          </div>
        )}
      </div>
      <div className="tab-panel">{children}</div>
    </div>);

}

// ---------- ProgressBar ----------
function ProgressBar({ marquee = false, value = 0 /* 0-100 */, width = 200 }) {
  const blocks = Math.floor(width / 12);
  return (
    <div className="progress" style={{ width }}>
      <div className={`progress__track ${marquee ? "progress__track--marquee" : ""}`}>
        {Array.from({ length: marquee ? 4 : Math.round(value / 100 * blocks) }).map((_, i) =>
        <div key={i} className="progress__block" />
        )}
      </div>
    </div>);

}

// ---------- DataGrid (simple) ----------
function DataGrid({ columns, rows, selectedId, onSelect, height = 220, getRowKey = (r) => r.id, getRowClass, emptyText = "No items" }) {
  return (
    <div className="grid" style={{ height, overflow: "auto" }}>
      {rows.length === 0 ?
      <div className="grid--no-rows">{emptyText}</div> :

      <table>
          <thead>
            <tr>
              {columns.map((c) => <th key={c.key} style={{ width: c.width }}>{c.label}</th>)}
            </tr>
          </thead>
          <tbody>
            {rows.map((r) => {
            const k = getRowKey(r);
            const isSel = k === selectedId;
            return (
              <tr
                key={k}
                className={`${isSel ? "is-selected" : ""} ${getRowClass ? getRowClass(r) : ""}`}
                onClick={() => onSelect && onSelect(k)}
                onDoubleClick={() => onSelect && onSelect(k, true)}>
                
                  {columns.map((c) =>
                <td key={c.key} style={{ textAlign: c.align }}>{c.render ? c.render(r) : r[c.key]}</td>
                )}
                </tr>);

          })}
          </tbody>
        </table>
      }
    </div>);

}

// ---------- Field row helper ----------
function Field({ label, children, labelWidth = 90, align = "center" }) {
  return (
    <div style={{ display: "flex", alignItems: align, gap: 8, marginBottom: 6 }}>
      <div style={{ width: labelWidth, flex: `0 0 ${labelWidth}px` }}>{label}</div>
      <div style={{ flex: 1 }}>{children}</div>
    </div>);

}

Object.assign(window, {
  W95Window, Btn, GroupBox, Checkbox, Radio, TextField, NumberField,
  Combo, MenuBar, StatusBar, Tabs, ProgressBar, DataGrid, Field, renderAccel
});