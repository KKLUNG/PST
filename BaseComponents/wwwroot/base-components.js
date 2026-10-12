// ============================================
// 底層元件鍵盤操作 (BaseComponents)
// 引用方式：<script src="_content/BaseComponents/base-components.js"></script>
//
// 1. 在 .base-keyboard-nav 範圍 (BaseQueryPanel、BaseFormDialog) 內的欄位按 Enter：
//    跳到下一個欄位；最後一個欄位則移到主要按鈕 (查詢、儲存)，再按 Enter 即執行
// 2. 設定 data-auto-next 的文字方框 (BaseTextBox 有 MaxLength 時) 輸入滿長度後自動跳到下一個欄位，
//    適合代碼、條碼機輸入
// ============================================
(function () {
    // Fluent UI 元件外層標籤 + 原生輸入元件；多行文字 (fluent-text-area) 不在此列，Enter 為換行
    const FIELD_SELECTOR = [
        'fluent-text-field', 'fluent-number-field', 'fluent-select', 'fluent-combobox',
        'fluent-checkbox', 'fluent-radio-group',
        'input:not([type=hidden]):not([type=button]):not([type=submit])', 'select'
    ].join(',');

    function isUsable(el) {
        return !el.hasAttribute('disabled')
            && !el.hasAttribute('readonly')
            && !el.disabled
            && !el.readOnly
            && el.offsetParent !== null;
    }

    // 取得範圍內可輸入的欄位 (排除包在其他欄位元件裡的子元素)
    function getFields(container) {
        return [...container.querySelectorAll(FIELD_SELECTOR)]
            .filter(el => isUsable(el) && !el.parentElement.closest(FIELD_SELECTOR));
    }

    function focusField(el) {
        if (el.tagName === 'FLUENT-RADIO-GROUP') {
            const radio = el.querySelector('fluent-radio[aria-checked="true"]') || el.querySelector('fluent-radio');
            (radio || el).focus();
            return;
        }

        el.focus();

        // 選取原有內容，直接輸入即可覆寫
        const input = el.shadowRoot ? el.shadowRoot.querySelector('input') : el;
        if (input && typeof input.select === 'function') {
            input.select();
        }
    }

    function focusNext(container, field) {
        const fields = getFields(container);
        const index = fields.indexOf(field);

        if (index >= 0 && index < fields.length - 1) {
            focusField(fields[index + 1]);
            return;
        }

        // 最後一個欄位：移到主要按鈕
        const primary = container.querySelector('[data-base-primary]');
        if (primary) {
            primary.focus();
        }
    }

    // 以捕獲階段攔截 Enter，避免欄位內按 Enter 直接送出表單
    document.addEventListener('keydown', function (e) {
        if (e.key !== 'Enter' || e.shiftKey || e.ctrlKey || e.altKey || e.isComposing) {
            return;
        }

        const target = e.target;
        const container = target.closest && target.closest('.base-keyboard-nav');
        if (!container) {
            return;
        }

        const field = target.closest(FIELD_SELECTOR);
        if (!field) {
            return; // 按鈕、多行文字等維持原本行為
        }

        // 下拉選單展開中：Enter 用於選取項目
        if ((field.tagName === 'FLUENT-SELECT' || field.tagName === 'FLUENT-COMBOBOX') && field.open) {
            return;
        }

        e.preventDefault();
        e.stopPropagation();
        focusNext(container, field);
    }, true);

    // 設定 data-digits-only 的欄位 (電話、手機) 只能輸入數字
    document.addEventListener('beforeinput', function (e) {
        const field = e.target;
        if (!field || !field.hasAttribute || !field.hasAttribute('data-digits-only')) {
            return;
        }

        if (e.data && /\D/.test(e.data)) {
            e.preventDefault();
        }
    }, true);

    // ---------- 自動轉大寫 (BaseTextBox UpperCase，data-uppercase) ----------
    // 在瀏覽器端立即轉換，避免等伺服器回傳大寫時覆蓋快速輸入 (例如條碼機) 的內容而掉字
    document.addEventListener('input', function (e) {
        const field = e.target;
        if (!field || !field.hasAttribute || !field.hasAttribute('data-uppercase') || e.isComposing) {
            return;
        }

        const input = e.composedPath()[0];
        if (!input || input === field || typeof input.value !== 'string') {
            return;
        }

        const upper = input.value.toUpperCase();
        if (upper !== input.value) {
            const caret = input.selectionStart;
            input.value = upper;
            input.setSelectionRange(caret, caret);
        }
    }, true);

    // ---------- 數字欄位 (BaseNumeric，data-numeric) ----------
    // 進入欄位：顯示不含千分位的原始數字，方便修改
    document.addEventListener('focusin', function (e) {
        const field = e.target;
        if (!field || !field.hasAttribute || !field.hasAttribute('data-numeric')) {
            return;
        }

        const raw = field.getAttribute('data-raw') || '';
        if (field.value !== raw) {
            field.value = raw;
        }
        const input = field.shadowRoot ? field.shadowRoot.querySelector('input') : null;
        if (input) {
            input.dataset.lastValid = raw;
            input.select();
        }
    }, true);

    // 離開欄位：未修改時恢復千分位格式；有修改時由伺服器回傳新的格式化結果
    document.addEventListener('focusout', function (e) {
        const field = e.target;
        if (!field || !field.hasAttribute || !field.hasAttribute('data-numeric')) {
            return;
        }

        if (field.value === (field.getAttribute('data-raw') || '')) {
            field.value = field.getAttribute('data-formatted') || '';
        }
    }, true);

    // 輸入限制：整數位數、小數位數、負號 (規則同 BaseNumberFormat.InputPattern)
    document.addEventListener('input', function (e) {
        const field = e.target;
        if (!field || !field.hasAttribute || !field.hasAttribute('data-numeric') || e.isComposing) {
            return;
        }

        const input = e.composedPath()[0];
        if (!input || input === field) {
            return;
        }

        const intDigits = parseInt(field.getAttribute('data-int-digits') || '15', 10);
        const decDigits = parseInt(field.getAttribute('data-dec-digits') || '0', 10);
        const sign = field.getAttribute('data-negative') === 'true' ? '-?' : '';
        const decimals = decDigits > 0 ? '(\\.\\d{0,' + decDigits + '})?' : '';
        const pattern = new RegExp('^' + sign + '\\d{0,' + intDigits + '}' + decimals + '$');

        if (pattern.test(input.value)) {
            input.dataset.lastValid = input.value;
        } else {
            // 不符合規則：恢復上一次正確的內容 (在 Fluent 元件讀取前修正)
            input.value = input.dataset.lastValid || '';
        }
    }, true);

    // 輸入滿長度自動跳下一個欄位
    document.addEventListener('input', function (e) {
        const field = e.target;
        if (!field || !field.hasAttribute || !field.hasAttribute('data-auto-next') || e.isComposing) {
            return;
        }

        // 捕獲階段時 Fluent 元件外層的 value 尚未更新，改讀實際輸入的 input 元素
        const input = e.composedPath()[0];
        const max = parseInt(field.getAttribute('maxlength') || '', 10);
        const value = (input && input.value) || '';
        if (max > 0 && value.length >= max) {
            focusNext(field.closest('.base-keyboard-nav') || document.body, field);
        }
    }, true);
})();
