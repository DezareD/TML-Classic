/**
 * @license Copyright (c) 2003-2019, CKSource - Frederico Knabben. All rights reserved.
 * For licensing, see https://ckeditor.com/legal/ckeditor-oss-license
 */
 
CKEDITOR.editorConfig = function( config ) {
	config.toolbar = [
        { name: 'document', groups: ['mode', 'document', 'doctools'], items: [ /*'Source', '-', 'Save', 'NewPage', 'Preview', 'Print', '-', 'Templates'*/ ] },
    //{ name: 'clipboard', groups: [ 'clipboard', 'undo' ], items: [ 'Cut', 'Copy', 'Paste', 'PasteText', 'PasteFromWord', '-', 'Undo', 'Redo' ] },
    //{ name: 'editing', groups: [ 'find', 'selection', 'spellchecker' ], items: [ 'Find', 'Replace', '-', 'SelectAll', '-', 'Scayt' ] },
    //{ name: 'forms', items: [ 'Form', 'Checkbox', 'Radio', 'TextField', 'Textarea', 'Select', 'Button', 'ImageButton', 'HiddenField' ] },
    { name: 'basicstyles', groups: [ 'basicstyles', 'cleanup' ], items: [ 'Bold', 'Italic', 'Underline', 'Strike',/* 'Subscript', 'Superscript', '-',*/ 'RemoveFormat' ] },
    { name: 'paragraph', groups: [ 'list', 'indent', 'blocks', 'align', 'bidi' ], items: [ 'NumberedList', 'BulletedList', /*'-', 'Outdent', 'Indent', '-',*/ 'Blockquote',/* 'CreateDiv', */'-', 'JustifyLeft', 'JustifyCenter', 'JustifyRight', 'JustifyBlock'/*, '-', 'BidiLtr', 'BidiRtl', 'Language'*/ ] },
    { name: 'links', items: [ 'Link', 'Unlink'/*, 'Anchor'*/ ] },
    { name: 'insert', items: [ 'Image' /*, 'Flash', 'Table', 'HorizontalRule', 'Smiley', 'SpecialChar', 'PageBreak', 'Iframe'*/ ] },
    { name: 'styles', items: [ /*'Styles', 'Format', 'Font',*/ 'FontSize' ] },
    { name: 'colors', items: [ 'TextColor', 'BGColor' ] },
    //{ name: 'tools', items: [ 'Maximize', 'ShowBlocks' ] },
    //{ name: 'others', items: [ '-' ] },
    //{ name: 'about', items: [ 'About' ] }
	]
	
    config.font_defaultLabel = 'Arial';

    config.filebrowserImageUploadUrl = '/upload_image_ckeditor' //Action for Uploding image  

    config.removePlugins = 'elementspath,magicline,image'; // autogrow resize
    config.extraPlugins = 'dialog,autogrow,lineutils,mentions,autocomplete,textmatch,autolink,uploadimage,image'; // image2 ,ajax,xml textwatcher,imageresizerowandcolumn

    config.imageUploadUrl = '/upload_image_ckeditor';

    config.cloudServices_tokenUrl = '';
    config.cloudServices_uploadUrl = '/upload_image_ckeditor';


	config.fullPage = false;
	config.resize_enabled = false;
    config.allowedContent = true;
    config.autoGrow_maxHeight = 400;

    config.fontSize_sizes = '9/9px;10/10px;11/11px;12/12px;14/14px;16/16px;18/18px;20/20px;24/24px;26/26px;28/28px;32/32px';

	
	config.width = 500;
    config.width = '100%';
};
