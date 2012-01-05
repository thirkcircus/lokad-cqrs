function Graph() {

    var ctx;
    var index = 0;
    var xMax = 400;
    var yMax = 250;
    var x = 0;
    var xOffset = 20;
    var yOffset = 20;
    this.Ready = false;

    var step = 1;
    var data = new Array();

    this.AddPoint = function(y) {
        if (x > xMax) {
            x = 0;
            index = 0;
        }

        data[index] = { x: x, y: y };
        index++;
        x += step;

        this.clearGraph(index);
    };

    this.Draw = function() {
        ctx.clearRect(-1 * xOffset, 0, xMax, yMax);

        ctx.moveTo(0, 0);

        ctx.save();
        ctx.beginPath();
        ctx.strokeStyle = 'black';
        for (var i = 0; i < data.length; i++) {
            var value = data[i];

            if (value.y == -1) {
                ctx.moveTo(value.x, yMax - yOffset);
            } else {
                ctx.lineTo(value.x, yMax - value.y - yOffset);
            }
        }
        ctx.stroke();

        ctx.save();
        ctx.beginPath();
        ctx.strokeStyle = 'green';

        var point = data[index - 1];
        ctx.arc(point.x, yMax - point.y - yOffset, 3, 0, 2 * Math.PI, false);
        ctx.fillStyle = 'green';
        ctx.fill();
        ctx.restore();

        this.DrawGrid();
    };

    this.DrawGrid = function() {
        ctx.save();
        ctx.strokeText('100', -1 * xOffset, yMax - 100 - 10);
        ctx.strokeText('200', -1 * xOffset, yMax - 200 - 10);
        ctx.restore();

        ctx.save();
        ctx.beginPath();

        ctx.lineWidth = 2;
        // x axis
        ctx.moveTo(0, yMax - yOffset);
        ctx.lineTo(xMax, yMax - yOffset);

        // y axis
        ctx.moveTo(0, yMax - 0);
        ctx.lineTo(0, 0);
        ctx.stroke();

        ctx.lineWidth = 1;

        ctx.strokeStyle = 'gray';
        // 100 msg /sec line
        ctx.moveTo(0, yMax - 100 - yOffset);
        ctx.lineTo(xMax, yMax - 100 - yOffset);

        // 200 msg /sec line
        ctx.moveTo(0, yMax - 200 - yOffset);
        ctx.lineTo(xMax, yMax - 200 - yOffset);
        ctx.stroke();

        ctx.restore();
    };

    this.clearGraph = function(start) {
        for (var i = start; i < start + 10; i++) {
            var p = data[i];
            if (p == null) break;

            data[i] = { x: p.x, y: -1 };
        }
    };

    this.Init = function (canvasId) {
        var canvas = document
            .getElementById(canvasId);

        if (canvas && canvas.getContext) {
            ctx = canvas.getContext('2d');
            ctx.translate(xOffset, 0);
            this.Ready = true;
        }
    };
}