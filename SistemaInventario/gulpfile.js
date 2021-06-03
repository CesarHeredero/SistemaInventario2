'use strict';

var gulp = require('gulp');
var sass = require('gulp-sass');

sass.compiler = require('node-sass');

gulp.task('build-css', function () {
    return gulp.src('./wwwroot/sass/**/*.scss')
        .pipe(sass())
        .pipe(gulp.dest('./wwwroot/css'));
});

gulp.task('default', function () {
    gulp.watch('./wwwroot/sass/**/*.scss', gulp.series('build-css'));
});