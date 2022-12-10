function SubmitUserScore(score) {
    if (score == 0) return;
    console.log("score:", score);
    $.post(`${base_url}/Home/SaveUserScore`, { score: score });
}