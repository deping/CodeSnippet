this.countDown = 5 * 60/*秒*/;
const setCodeTitle = () => {
    this.codeTitle = "等待" + this.countDown + "秒获取验证码";
};
setCodeTitle();
const timerId = setInterval(() => {
    --this.countDown;
    if (this.countDown === 0) {
        this.codeTitle = "获取验证码";
        clearInterval(timerId);
    } else {
        setCodeTitle();
    }
}, 1000);
