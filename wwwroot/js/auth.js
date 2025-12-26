window.mailsharp = window.mailsharp || {};

window.mailsharp.postJson = async function (url, data) {
    const resp = await fetch(url, {
        method: "POST",
        headers: {
            "Content-Type": "application/json"
        },
        credentials: "same-origin", // garante envio/recebimento de cookies no mesmo domínio
        body: JSON.stringify(data)
    });

    const text = await resp.text();
    return {
        status: resp.status,
        ok: resp.ok,
        body: text
    };
};